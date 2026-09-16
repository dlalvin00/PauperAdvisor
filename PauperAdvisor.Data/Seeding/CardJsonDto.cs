using System.Text.Json.Serialization;

namespace PauperAdvisor.Data.Seeding;

public class CardJsonDto
{
    [JsonPropertyName("oracle_id")] public Guid OracleId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("mana_cost")] public string? ManaCost { get; set; }
    [JsonPropertyName("cmc")] public decimal Cmc { get; set; }
    [JsonPropertyName("type_line")] public string TypeLine { get; set; } = "";
    [JsonPropertyName("oracle_text")] public string? OracleText { get; set; }
    [JsonPropertyName("colors")] public List<string> Colors { get; set; } = [];
    [JsonPropertyName("color_identity")] public List<string> ColorIdentity { get; set; } = [];
    [JsonPropertyName("keywords")] public List<string> Keywords { get; set; } = [];
    [JsonPropertyName("pauper_legality")] public string PauperLegality { get; set; } = "";
    [JsonPropertyName("layout")] public string Layout { get; set; } = "";
}