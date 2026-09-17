namespace PauperAdvisor.Domain.Decks;

public sealed class DeckList
{
    public DeckList(IReadOnlyList<DeckCard> cards)
    {
        Cards = cards;
    }

    public IReadOnlyList<DeckCard> Cards { get; }

    public int TotalCards => Cards.Sum(card => card.Quantity);
}