namespace PauperAdvisor.Domain.Entities;

public class Ruling
{
    public Guid Id { get; set; }
    public Guid CardOracleId { get; set; }

    public required string Source { get; set; }
    public DateTime PublishedAt { get; set; }
    public required string Comment { get; set; }

    public Card Card { get; set; } = null!;
}