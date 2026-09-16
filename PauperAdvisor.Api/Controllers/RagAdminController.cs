using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.RAG.Services;
using PauperAdvisor.RAG.Storage;

namespace PauperAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RagAdminController(
    IServiceScopeFactory scopeFactory,
    IngestionStatusService statusService) : ControllerBase
{
    // Endpoint para consultar o progresso a qualquer momento
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(statusService);
    }

    [HttpPost("build-knowledge-base")]
    public IActionResult BuildKnowledgeBase()
    {
        if (statusService.IsRunning)
            return BadRequest(new { Message = "A ingestão já está em andamento." });

        statusService.IsRunning = true;
        statusService.Message = "Iniciando leitura do banco de dados...";
        statusService.ProcessedCards = 0;
        statusService.TotalCards = 0;

        // Dispara o processo em background e libera a requisição HTTP imediatamente
        _ = Task.Run(async () => await ProcessIngestionAsync());

        return Accepted(new { Message = "Processo em background iniciado. Consulte /api/RagAdmin/status para o progresso." });
    }

    [HttpGet("test-retrieval")]
    public async Task<IActionResult> TestRetrieval([FromQuery] string question, [FromServices] RetrievalService retrievalService)
    {
        // Se a ingestão ainda estiver rodando, avisa o usuário
        if (statusService.IsRunning)
            return BadRequest("Aguarde o término da ingestão vetorial antes de testar a busca.");

        var context = await retrievalService.RetrieveContextAsync(question);

        return Ok(new
        {
            Question = question,
            GeneratedContext = context
        });
    }

    private async Task ProcessIngestionAsync()
    {
        // Cria um escopo de injeção de dependência independente da requisição HTTP
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var qdrantService = scope.ServiceProvider.GetRequiredService<QdrantStorageService>();

        try
        {
            await qdrantService.InitializeCollectionAsync();

            var cards = await dbContext.Cards.Include(c => c.Rulings).ToListAsync();

            statusService.TotalCards = cards.Count;
            statusService.Message = "Gerando embeddings e enviando para o Qdrant...";

            foreach (var card in cards)
            {
                var cardText = $"Type: Card | Name: {card.Name} | Cost: {card.ManaCost} | TypeLine: {card.TypeLine} | Text: {card.OracleText}";
                var cardVector = await embeddingService.GenerateEmbeddingAsync(cardText);

                var cardPayload = new Dictionary<string, string>
                {
                    { "oracle_id", card.OracleId.ToString() },
                    { "doc_type", "Card" },
                    { "name", card.Name }
                };

                await qdrantService.UpsertPointAsync(card.OracleId, cardVector, cardPayload);

                foreach (var ruling in card.Rulings)
                {
                    var rulingText = $"Type: Ruling | Card: {card.Name} | Rule: {ruling.Comment}";
                    var rulingVector = await embeddingService.GenerateEmbeddingAsync(rulingText);

                    var rulingPayload = new Dictionary<string, string>
                    {
                        { "oracle_id", card.OracleId.ToString() },
                        { "doc_type", "Ruling" },
                        { "name", card.Name }
                    };

                    await qdrantService.UpsertPointAsync(ruling.Id, rulingVector, rulingPayload);
                }

                statusService.ProcessedCards++;

                if (statusService.ProcessedCards % 50 == 0)
                    Console.WriteLine($"[Qdrant Ingest] {statusService.Percentage}% concluído...");
            }

            statusService.Message = "Ingestão concluída com sucesso!";
        }
        catch (Exception ex)
        {
            statusService.Message = $"Erro crítico na ingestão: {ex.Message}";
            Console.WriteLine(ex);
        }
        finally
        {
            statusService.IsRunning = false;
        }
    }
}