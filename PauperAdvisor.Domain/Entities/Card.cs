// Em PauperAdvisor.Domain.Entities
using PauperAdvisor.Domain.Entities;
using System.Runtime.InteropServices;

public class Card
{
    public required Guid OracleId { get; set; }
    public required string Name { get; set; }
    public string? ManaCost { get; set; }
    public decimal Cmc { get; set; }
    public required string TypeLine { get; set; }
    public string? OracleText { get; set; }

    public List<string> Colors { get; set; } = []; // Sintaxe de coleção simplificada do C#
    public List<string> ColorIdentity { get; set; } = [];
    public List<string> Keywords { get; set; } = [];

    public required string PauperLegality { get; set; }
    public required string Layout { get; set; }

    public List<CardFace> CardFaces { get; set; } = [];
    public List<Ruling> Rulings { get; set; } = [];
}