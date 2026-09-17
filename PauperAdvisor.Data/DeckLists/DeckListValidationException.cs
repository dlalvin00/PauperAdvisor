namespace PauperAdvisor.Data.DeckLists;

public sealed class DeckListValidationException : Exception
{
    public DeckListValidationException(IEnumerable<string> errors)
        : base("The deck list contains validation errors.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}