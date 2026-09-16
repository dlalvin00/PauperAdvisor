using PauperAdvisor.RAG.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace PauperAdvisor.RAG.Storage;

public class QdrantStorageService
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantStorageService(QdrantOptions options)
    {
        _options = options;
        _client = new QdrantClient(options.Host, options.GrpcPort);
    }

    public async Task InitializeCollectionAsync()
    {
        var collections = await _client.ListCollectionsAsync();

        if (!collections.Contains(_options.CollectionName))
        {
            await _client.CreateCollectionAsync(
                collectionName: _options.CollectionName,
                vectorsConfig: new VectorParams
                {
                    Size = _options.VectorSize,
                    Distance = Distance.Cosine
                }
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

        foreach (var item in payload)
        {
            pointStruct.Payload.Add(item.Key, item.Value);
        }

        await _client.UpsertAsync(_options.CollectionName, new[] { pointStruct });
    }

    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(float[] vector, int limit = 20)
    {
        return await _client.QueryAsync(
            collectionName: _options.CollectionName,
            query: vector,
            limit: (ulong)limit
        );
    }
}
