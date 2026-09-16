using System.Text.Json.Serialization;

namespace PauperAdvisor.Data.Seeding;

public class RulingJsonDto
{
    [JsonPropertyName("oracle_id")] public Guid OracleId { get; set; }
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("published_at")] public DateTime PublishedAt { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
}