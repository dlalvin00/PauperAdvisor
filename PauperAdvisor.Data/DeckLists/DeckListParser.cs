using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Domain.Decks;

namespace PauperAdvisor.Data.DeckLists;

public sealed class DeckListParser(ApplicationDbContext dbContext)
{
    private static readonly Regex DeckLinePattern = new(
        @"^(?:SB:\s*)?(?<quantity>[+-]?\d+)\s*(?:[xX×]\s*)?(?<name>\S.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ExportSuffixPattern = new(
        @"\s+(?:\([A-Za-z0-9]{2,8}\)|\[[A-Za-z0-9]{2,8}\])\s+[A-Za-z0-9★]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FaceSeparatorPattern = new(
        @"\s*/{1,2}\s*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespacePattern = new(
        @"\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private CardCatalog? _catalog;

    public async Task<DeckList> ParseAndValidateAsync(
        string deckListText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deckListText))
        {
            throw new DeckListValidationException([
                "The deck list cannot be empty."
            ]);
        }

        var errors = new List<string>();

        var parsedLines = ParseLines(
            deckListText,
            errors);

        if (errors.Count > 0)
            throw new DeckListValidationException(errors);

        var catalog = await GetCatalogAsync(
            cancellationToken);

        var resolvedCards =
            new Dictionary<Guid, ResolvedDeckCard>();

        foreach (var line in parsedLines)
        {
            var resolution =
                catalog.Resolve(line.CardName);

            if (resolution.Status ==
                CardResolutionStatus.NotFound)
            {
                errors.Add(
                    $"Line {line.LineNumber}: card '{line.CardName}' was not found in the Pauper card database.");

                continue;
            }

            if (resolution.Status ==
                CardResolutionStatus.Ambiguous)
            {
                var candidates = string.Join(
                    ", ",
                    resolution.Candidates.Select(
                        card => $"'{card.Name}'"));

                errors.Add(
                    $"Line {line.LineNumber}: card name '{line.CardName}' is ambiguous. Use one of the full canonical names: {candidates}.");

                continue;
            }

            var card = resolution.Candidates[0];

            if (resolvedCards.TryGetValue(
                    card.OracleId,
                    out var existingCard))
            {
                try
                {
                    resolvedCards[card.OracleId] =
                        existingCard with
                        {
                            Quantity = checked(
                                existingCard.Quantity +
                                line.Quantity)
                        };
                }
                catch (OverflowException)
                {
                    errors.Add(
                        $"Line {line.LineNumber}: the total quantity for '{card.Name}' exceeds the supported maximum.");
                }

                continue;
            }

            resolvedCards.Add(
                card.OracleId,
                new ResolvedDeckCard(
                    card.OracleId,
                    card.Name,
                    line.Quantity,
                    IsLand(card.TypeLine)));
        }

        if (errors.Count > 0)
        {
            throw new DeckListValidationException(
                errors.Distinct().ToArray());
        }

        var cards = resolvedCards.Values
            .OrderBy(
                card => card.Name,
                StringComparer.OrdinalIgnoreCase)
            .Select(card => new DeckCard(
                card.OracleId,
                card.Name,
                card.Quantity,
                card.IsLand))
            .ToArray();

        return new DeckList(cards);
    }

    private async Task<CardCatalog> GetCatalogAsync(
        CancellationToken cancellationToken)
    {
        if (_catalog is not null)
            return _catalog;

        var cards = await dbContext.Cards
            .AsNoTracking()
            .Select(card => new
            {
                card.OracleId,
                card.Name,
                card.TypeLine
            })
            .ToListAsync(cancellationToken);

        var faceNames = await dbContext.CardFaces
            .AsNoTracking()
            .Select(face => new
            {
                face.CardOracleId,
                face.Name
            })
            .ToListAsync(cancellationToken);

        var faceNamesByCard = faceNames
            .GroupBy(face => face.CardOracleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(face => face.Name)
                    .ToArray());

        var entries = cards
            .Select(card => new CardCatalogEntry(
                card.OracleId,
                card.Name,
                card.TypeLine,
                faceNamesByCard.GetValueOrDefault(
                    card.OracleId,
                    Array.Empty<string>())))
            .ToArray();

        _catalog = new CardCatalog(entries);

        return _catalog;
    }

    private static List<ParsedDeckLine> ParseLines(
        string deckListText,
        ICollection<string> errors)
    {
        var parsedLines =
            new List<ParsedDeckLine>();

        var lines = deckListText
            .Replace(
                "\r\n",
                "\n",
                StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        for (var index = 0;
             index < lines.Length;
             index++)
        {
            var lineNumber = index + 1;
            var line = lines[index].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match =
                DeckLinePattern.Match(line);

            if (!match.Success)
            {
                errors.Add(
                    $"Line {lineNumber}: expected the format '<quantity> <card name>', for example '4 Lightning Bolt'.");

                continue;
            }

            var quantityText =
                match.Groups["quantity"].Value;

            var cardName =
                match.Groups["name"].Value.Trim();

            if (!int.TryParse(
                    quantityText,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var quantity)
                || quantity <= 0)
            {
                errors.Add(
                    $"Line {lineNumber}: quantity '{quantityText}' must be a positive integer.");

                continue;
            }

            cardName =
                StripExportMetadata(cardName);

            if (string.IsNullOrWhiteSpace(cardName))
            {
                errors.Add(
                    $"Line {lineNumber}: card name cannot be empty.");

                continue;
            }

            parsedLines.Add(
                new ParsedDeckLine(
                    lineNumber,
                    quantity,
                    cardName));
        }

        if (parsedLines.Count == 0 &&
            errors.Count == 0)
        {
            errors.Add(
                "The deck list does not contain any cards.");
        }

        return parsedLines;
    }

    private static string StripExportMetadata(
        string cardName)
    {
        return ExportSuffixPattern
            .Replace(cardName, string.Empty)
            .Trim();
    }

    private static string NormalizeCardName(
        string value)
    {
        var normalized = value
            .Normalize(NormalizationForm.FormKC)
            .Replace('’', '\'')
            .Replace('‘', '\'')
            .Replace('“', '"')
            .Replace('”', '"')
            .Trim();

        normalized =
            FaceSeparatorPattern.Replace(
                normalized,
                " // ");

        normalized =
            WhitespacePattern.Replace(
                normalized,
                " ");

        return normalized;
    }

    private static bool IsLand(
        string typeLine)
    {
        return typeLine.Contains(
            "Land",
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CardCatalog
    {
        private readonly Dictionary<
            string,
            CardCatalogEntry[]> _canonicalNames;

        private readonly Dictionary<
            string,
            CardCatalogEntry[]> _aliases;

        public CardCatalog(
            IEnumerable<CardCatalogEntry> cards)
        {
            var entries = cards.ToArray();

            _canonicalNames = entries
                .GroupBy(
                    card =>
                        NormalizeCardName(card.Name),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .DistinctBy(card => card.OracleId)
                        .ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            _aliases = entries
                .SelectMany(card =>
                    GetAliases(card).Select(alias => new
                    {
                        Alias = alias,
                        Card = card
                    }))
                .GroupBy(
                    entry => entry.Alias,
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(entry => entry.Card)
                        .DistinctBy(card => card.OracleId)
                        .OrderBy(
                            card => card.Name,
                            StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    StringComparer.OrdinalIgnoreCase);
        }

        public CardResolution Resolve(
            string inputName)
        {
            var normalizedName =
                NormalizeCardName(inputName);

            if (_canonicalNames.TryGetValue(
                    normalizedName,
                    out var canonicalMatches))
            {
                return ToResolution(
                    canonicalMatches);
            }

            return _aliases.TryGetValue(
                normalizedName,
                out var aliasMatches)
                ? ToResolution(aliasMatches)
                : new CardResolution(
                    CardResolutionStatus.NotFound,
                    []);
        }

        private static CardResolution ToResolution(
            CardCatalogEntry[] matches)
        {
            return matches.Length == 1
                ? new CardResolution(
                    CardResolutionStatus.Found,
                    matches)
                : new CardResolution(
                    CardResolutionStatus.Ambiguous,
                    matches);
        }

        private static IEnumerable<string> GetAliases(
            CardCatalogEntry card)
        {
            var canonicalName =
                NormalizeCardName(card.Name);

            var aliases =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var faceName in card.FaceNames)
            {
                aliases.Add(
                    NormalizeCardName(faceName));
            }

            if (canonicalName.Contains(
                    " // ",
                    StringComparison.Ordinal))
            {
                foreach (var faceName in canonicalName.Split(
                             " // ",
                             StringSplitOptions.RemoveEmptyEntries |
                             StringSplitOptions.TrimEntries))
                {
                    aliases.Add(faceName);
                }
            }

            aliases.Remove(canonicalName);

            return aliases;
        }
    }

    private sealed record ParsedDeckLine(
        int LineNumber,
        int Quantity,
        string CardName);

    private sealed record CardCatalogEntry(
        Guid OracleId,
        string Name,
        string TypeLine,
        IReadOnlyList<string> FaceNames);

    private sealed record ResolvedDeckCard(
        Guid OracleId,
        string Name,
        int Quantity,
        bool IsLand);

    private sealed record CardResolution(
        CardResolutionStatus Status,
        IReadOnlyList<CardCatalogEntry> Candidates);

    private enum CardResolutionStatus
    {
        NotFound,
        Found,
        Ambiguous
    }
}