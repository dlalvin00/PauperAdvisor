using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace PauperAdvisor.RAG.Storage;

public class QdrantStorageService
{
    private readonly QdrantClient _client;
    public const string CollectionName = "pauper_knowledge";

    public QdrantStorageService()
    {
        // Conecta ao container local do Qdrant configurado anteriormente
        _client = new QdrantClient("localhost", 6334);
    }

    public async Task InitializeCollectionAsync()
    {
        var collections = await _client.ListCollectionsAsync();

        if (!collections.Contains(CollectionName))
        {
            // Cria a coleção para o modelo nomic-embed-text (768 dimensões)
            // Utiliza a métrica de Cosseno, ideal para similaridade semântica de textos
            await _client.CreateCollectionAsync(
                collectionName: CollectionName,
                vectorsConfig: new VectorParams { Size = 768, Distance = Distance.Cosine }
            );
        }
    }

    public async Task UpsertPointAsync(Guid id, float[] vector, Dictionary<string, string> payload)
    {
        var pointStruct = new PointStruct
        {
            Id = id,
            Vectors = vector,
        };

        // Adiciona a metadata (ex: OracleId, Tipo, Texto original)
        foreach (var item in payload)
        {
            pointStruct.Payload.Add(item.Key, item.Value);
        }

        await _client.UpsertAsync(CollectionName, new[] { pointStruct });
    }

    public async Task<IReadOnlyList<Qdrant.Client.Grpc.ScoredPoint>> SearchAsync(float[] vector, int limit = 20)
    {
        // Realiza a busca por similaridade de cosseno usando o vetor da pergunta
        return await _client.QueryAsync(
            collectionName: CollectionName,
            query: vector,
            limit: (ulong)limit
        );
    }
}