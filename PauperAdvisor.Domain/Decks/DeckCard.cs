namespace PauperAdvisor.Domain.Decks;

public sealed record DeckCard(
    Guid OracleId,
    string Name,
    int Quantity,
    bool IsLand);