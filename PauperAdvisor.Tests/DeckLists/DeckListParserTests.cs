using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.Data.DeckLists;
using PauperAdvisor.Domain.Entities;
using Xunit;

namespace PauperAdvisor.Tests.DeckLists;

public sealed class DeckListParserTests
{
    [Fact]
    public async Task ParseAndValidateAsync_ResolvesCardsAndIdentifiesLands()
    {
        var lightningBolt =
            CreateCard("Lightning Bolt", "Instant");

        var island =
            CreateCard("Island", "Basic Land — Island");

        await using var database =
            await TestDatabase.CreateAsync(
                lightningBolt,
                island);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                "4 Lightning Bolt\n20 Island");

        Assert.Equal(24, deck.TotalCards);

        Assert.Collection(
            deck.Cards,
            card =>
            {
                Assert.Equal("Island", card.Name);
                Assert.Equal(20, card.Quantity);
                Assert.True(card.IsLand);
            },
            card =>
            {
                Assert.Equal("Lightning Bolt", card.Name);
                Assert.Equal(4, card.Quantity);
                Assert.False(card.IsLand);
            });
    }

    [Fact]
    public async Task ParseAndValidateAsync_ConsolidatesCaseAndExportVariants()
    {
        var lightningBolt =
            CreateCard("Lightning Bolt", "Instant");

        await using var database =
            await TestDatabase.CreateAsync(
                lightningBolt);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                """
                2 lightning bolt
                2x LIGHTNING BOLT (M11) 149
                """);

        var card = Assert.Single(deck.Cards);

        Assert.Equal(
            lightningBolt.OracleId,
            card.OracleId);

        Assert.Equal(
            "Lightning Bolt",
            card.Name);

        Assert.Equal(4, card.Quantity);
    }

    [Fact]
    public async Task ParseAndValidateAsync_AcceptsSideboardExportPrefix()
    {
        var pyroblast =
            CreateCard("Pyroblast", "Instant");

        await using var database =
            await TestDatabase.CreateAsync(
                pyroblast);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                "SB: 2 Pyroblast");

        var card = Assert.Single(deck.Cards);

        Assert.Equal("Pyroblast", card.Name);
        Assert.Equal(2, card.Quantity);
    }

    [Theory]
    [InlineData("1 Fire // Ice")]
    [InlineData("1 fire/ice")]
    [InlineData("1 FIRE / ICE")]
    [InlineData("1 Fire  //  Ice")]
    public async Task ParseAndValidateAsync_NormalizesMultifaceSeparators(
        string deckLine)
    {
        var fireIce =
            CreateCard(
                "Fire // Ice",
                "Instant // Instant");

        await using var database =
            await TestDatabase.CreateAsync(
                fireIce);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                deckLine);

        var card = Assert.Single(deck.Cards);

        Assert.Equal(
            "Fire // Ice",
            card.Name);
    }

    [Fact]
    public async Task ParseAndValidateAsync_ResolvesUniqueFaceNameToCanonicalCard()
    {
        var balaGedRecovery =
            CreateCard(
                "Bala Ged Recovery // Bala Ged Sanctuary",
                "Sorcery // Land");

        await using var database =
            await TestDatabase.CreateAsync(
                balaGedRecovery);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                "2 Bala Ged Recovery");

        var card = Assert.Single(deck.Cards);

        Assert.Equal(
            "Bala Ged Recovery // Bala Ged Sanctuary",
            card.Name);

        Assert.Equal(2, card.Quantity);
        Assert.True(card.IsLand);
    }

    [Fact]
    public async Task ParseAndValidateAsync_PrefersCanonicalNameOverFaceAlias()
    {
        var memory =
            CreateCard("Memory", "Sorcery");

        var commitMemory =
            CreateCard(
                "Commit // Memory",
                "Instant // Sorcery");

        await using var database =
            await TestDatabase.CreateAsync(
                memory,
                commitMemory);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                "1 Memory");

        var card = Assert.Single(deck.Cards);

        Assert.Equal(
            memory.OracleId,
            card.OracleId);

        Assert.Equal("Memory", card.Name);
    }

    [Fact]
    public async Task ParseAndValidateAsync_RejectsAmbiguousFaceAlias()
    {
        var firstCard =
            CreateCard(
                "First Front // Shared Back",
                "Instant // Sorcery");

        var secondCard =
            CreateCard(
                "Second Front // Shared Back",
                "Creature // Sorcery");

        await using var database =
            await TestDatabase.CreateAsync(
                firstCard,
                secondCard);

        var exception =
            await Assert.ThrowsAsync<
                DeckListValidationException>(
                () => database.Parser
                    .ParseAndValidateAsync(
                        "1 Shared Back"));

        var error =
            Assert.Single(exception.Errors);

        Assert.Contains(
            "is ambiguous",
            error);

        Assert.Contains(
            "First Front // Shared Back",
            error);

        Assert.Contains(
            "Second Front // Shared Back",
            error);
    }

    [Fact]
    public async Task ParseAndValidateAsync_ConsolidatesCanonicalAndFaceAlias()
    {
        var adventure =
            CreateCard(
                "Beanstalk Giant // Fertile Footsteps",
                "Creature — Giant // Sorcery — Adventure");

        await using var database =
            await TestDatabase.CreateAsync(
                adventure);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                """
                1 Beanstalk Giant
                2 Beanstalk Giant // Fertile Footsteps
                """);

        var card = Assert.Single(deck.Cards);

        Assert.Equal(3, card.Quantity);

        Assert.Equal(
            adventure.OracleId,
            card.OracleId);
    }

    [Fact]
    public async Task ParseAndValidateAsync_UsesStoredCardFacesAsAliases()
    {
        var delver =
            CreateCard("Delver Card", "Creature");

        var cardFace = new CardFace
        {
            Id = Guid.NewGuid(),
            CardOracleId = delver.OracleId,
            FaceIndex = 0,
            Name = "Delver Front",
            TypeLine = "Creature"
        };

        await using var database =
            await TestDatabase.CreateAsync(
                [delver],
                [cardFace]);

        var deck =
            await database.Parser.ParseAndValidateAsync(
                "4 Delver Front");

        var card = Assert.Single(deck.Cards);

        Assert.Equal(
            "Delver Card",
            card.Name);

        Assert.Equal(4, card.Quantity);
    }

    [Fact]
    public async Task ParseAndValidateAsync_ReportsEveryUnknownCard()
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var exception =
            await Assert.ThrowsAsync<
                DeckListValidationException>(
                () => database.Parser
                    .ParseAndValidateAsync(
                        """
                        1 Imaginary Card
                        2 Another Imaginary Card
                        """));

        Assert.Equal(
            2,
            exception.Errors.Count);

        Assert.Contains(
            "Line 1",
            exception.Errors[0]);

        Assert.Contains(
            "Line 2",
            exception.Errors[1]);
    }

    [Theory]
    [InlineData("0 Lightning Bolt")]
    [InlineData("-1 Lightning Bolt")]
    [InlineData("999999999999 Lightning Bolt")]
    public async Task ParseAndValidateAsync_RejectsInvalidQuantities(
        string deckLine)
    {
        var lightningBolt =
            CreateCard("Lightning Bolt", "Instant");

        await using var database =
            await TestDatabase.CreateAsync(
                lightningBolt);

        var exception =
            await Assert.ThrowsAsync<
                DeckListValidationException>(
                () => database.Parser
                    .ParseAndValidateAsync(
                        deckLine));

        Assert.Contains(
            "must be a positive integer",
            Assert.Single(exception.Errors));
    }

    [Fact]
    public async Task ParseAndValidateAsync_RejectsMalformedLines()
    {
        var lightningBolt =
            CreateCard("Lightning Bolt", "Instant");

        await using var database =
            await TestDatabase.CreateAsync(
                lightningBolt);

        var exception =
            await Assert.ThrowsAsync<
                DeckListValidationException>(
                () => database.Parser
                    .ParseAndValidateAsync(
                        "Lightning Bolt"));

        Assert.Contains(
            "expected the format",
            Assert.Single(exception.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public async Task ParseAndValidateAsync_RejectsEmptyDeckLists(
        string deckList)
    {
        await using var database =
            await TestDatabase.CreateAsync();

        var exception =
            await Assert.ThrowsAsync<
                DeckListValidationException>(
                () => database.Parser
                    .ParseAndValidateAsync(
                        deckList));

        Assert.Contains(
            "cannot be empty",
            Assert.Single(exception.Errors));
    }

    private static Card CreateCard(
        string name,
        string typeLine)
    {
        return new Card
        {
            OracleId = Guid.NewGuid(),
            Name = name,
            TypeLine = typeLine,
            PauperLegality = "legal",
            Layout = "normal"
        };
    }

    private sealed class TestDatabase :
        IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private readonly ApplicationDbContext _dbContext;

        private TestDatabase(
            SqliteConnection connection,
            ApplicationDbContext dbContext)
        {
            _connection = connection;
            _dbContext = dbContext;

            Parser =
                new DeckListParser(dbContext);
        }

        public DeckListParser Parser { get; }

        public static Task<TestDatabase> CreateAsync(
            params Card[] cards)
        {
            return CreateAsync(cards, []);
        }

        public static async Task<TestDatabase> CreateAsync(
            IReadOnlyCollection<Card> cards,
            IReadOnlyCollection<CardFace> cardFaces)
        {
            var connection =
                new SqliteConnection(
                    "Data Source=:memory:");

            await connection.OpenAsync();

            var options =
                new DbContextOptionsBuilder<
                        ApplicationDbContext>()
                    .UseSqlite(connection)
                    .Options;

            var dbContext =
                new ApplicationDbContext(options);

            await dbContext.Database
                .EnsureCreatedAsync();

            dbContext.Cards.AddRange(cards);
            dbContext.CardFaces.AddRange(cardFaces);

            await dbContext.SaveChangesAsync();

            return new TestDatabase(
                connection,
                dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}