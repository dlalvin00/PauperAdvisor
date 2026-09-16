using OllamaSharp;
using OllamaSharp.Models;

namespace PauperAdvisor.RAG.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly OllamaApiClient _ollamaClient;
    private const string ModelName = "nomic-embed-text";

    public OllamaEmbeddingService()
    {
        _ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434"));
        _ollamaClient.SelectedModel = ModelName;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new EmbedRequest
        {
            Model = ModelName,
            // Colocamos a string dentro de uma lista para satisfazer a tipagem
            Input = new List<string> { text }
        };

        var response = await _ollamaClient.EmbedAsync(request);

        // Retorna o primeiro vetor gerado (já que enviamos apenas 1 string na lista)
        return response.Embeddings.First();
    }
}