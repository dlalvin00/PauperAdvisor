namespace PauperAdvisor.Domain.Entities;

public class CardFace
{
    public Guid Id { get; set; }
    public Guid CardOracleId { get; set; }

    public int FaceIndex { get; set; }
    public required string Name { get; set; }
    public string? ManaCost { get; set; }
    public required string TypeLine { get; set; }
    public string? OracleText { get; set; }

    public Card Card { get; set; } = null!;
}