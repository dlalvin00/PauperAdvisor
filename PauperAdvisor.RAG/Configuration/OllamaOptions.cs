namespace PauperAdvisor.RAG.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen2.5vl:7b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
