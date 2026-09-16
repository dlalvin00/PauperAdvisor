using System.Text;
using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;
using PauperAdvisor.RAG.Storage;

namespace PauperAdvisor.RAG.Services;

public class RetrievalService(
    IEmbeddingService embeddingService,
    QdrantStorageService qdrantService,
    ApplicationDbContext dbContext)
{
    public async Task<string> RetrieveContextAsync(string question, int limit = 20)
    {
        // 1. Converte a pergunta do usuário em um vetor de 768 dimensões
        var queryVector = await embeddingService.GenerateEmbeddingAsync(question);

        // 2. Busca os documentos mais semanticamente próximos no Qdrant
        var searchResults = await qdrantService.SearchAsync(queryVector, limit);

        if (!searchResults.Any())
            return "Nenhum contexto relevante encontrado.";

        // 3. Extrai os OracleIds únicos encontrados nos metadados do Qdrant
        var oracleIds = searchResults
            .Where(p => p.Payload.ContainsKey("oracle_id"))
            .Select(p => Guid.Parse(p.Payload["oracle_id"].StringValue))
            .Distinct()
            .ToList();

        // 4. Busca a "Verdade Absoluta" no SQLite usando os IDs recuperados
        var relevantCards = await dbContext.Cards
            .Include(c => c.Rulings)
            .Where(c => oracleIds.Contains(c.OracleId))
            .ToListAsync();

        // 5. Monta o bloco de contexto rigoroso que será injetado no prompt do Qwen3
        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("KNOWLEDGE BASE CONTEXT:");
        contextBuilder.AppendLine("-----------------------");

        foreach (var card in relevantCards)
        {
            contextBuilder.AppendLine($"CARD: {card.Name}");
            contextBuilder.AppendLine($"COST: {card.ManaCost} | TYPE: {card.TypeLine}");
            contextBuilder.AppendLine($"TEXT: {card.OracleText}");

            if (card.Rulings.Any())
            {
                contextBuilder.AppendLine("OFFICIAL RULINGS:");
                foreach (var ruling in card.Rulings)
                {
                    contextBuilder.AppendLine($"- [{ruling.PublishedAt:yyyy-MM-dd}] {ruling.Comment}");
                }
            }
            contextBuilder.AppendLine("-----------------------");
        }

        return contextBuilder.ToString();
    }

    public async Task<List<string>> ValidateCardNamesAsync(string rawOcrText)
    {
        // Limpa a string suja e separa por vírgulas
        var potentialNames = rawOcrText.Split(',')
            .Select(n => n.Trim().ToLower())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        // Busca no SQLite apenas as cartas cujos nomes batem com a extração
        // Para uma aplicação real, você pode implementar Fuzzy Search (Levenshtein) aqui futuramente
        var validCards = await dbContext.Cards
            .Where(c => potentialNames.Contains(c.Name.ToLower()))
            .Select(c => c.Name)
            .ToListAsync();

        return validCards;
    }
}