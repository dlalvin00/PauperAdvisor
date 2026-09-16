namespace PauperAdvisor.RAG.Configuration;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int GrpcPort { get; set; } = 6334;
    public string CollectionName { get; set; } = "pauper_knowledge";
    public ulong VectorSize { get; set; } = 768;
}
