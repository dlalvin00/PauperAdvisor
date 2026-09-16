using OllamaSharp;
using OllamaSharp.Models;
using PauperAdvisor.RAG.Configuration;

namespace PauperAdvisor.RAG.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly string _modelName;

    public OllamaEmbeddingService(OllamaOptions options)
    {
        _modelName = options.EmbeddingModel;
        _ollamaClient = new OllamaApiClient(new Uri(options.BaseUrl))
        {
            SelectedModel = _modelName
        };
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new EmbedRequest
        {
            Model = _modelName,
            Input = new List<string> { text }
        };

        var response = await _ollamaClient.EmbedAsync(request);
        return response.Embeddings.First();
    }
}
