using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using PauperAdvisor.Domain.Entities;

namespace PauperAdvisor.Data.Seeding;

public class DatabaseSeeder(ApplicationDbContext dbContext)
{
    public async Task SeedAsync(string cardsFilePath, string rulingsFilePath)
    {
        await dbContext.Database.MigrateAsync();

        if (await dbContext.Cards.AnyAsync())
        {
            Console.WriteLine("Banco já populado. Ignorando importação.");
            return;
        }

        Console.WriteLine("Iniciando importação de Cartas...");
        await ImportCardsAsync(cardsFilePath);

        Console.WriteLine("Iniciando importação de Rulings...");
        await ImportRulingsAsync(rulingsFilePath);

        Console.WriteLine("Importação concluída com sucesso!");
    }

    private async Task ImportCardsAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var cardDtos = await JsonSerializer.DeserializeAsync<List<CardJsonDto>>(stream) ?? [];

        var cardsToInsert = cardDtos.Select(dto => new Card
        {
            OracleId = dto.OracleId,
            Name = dto.Name,
            ManaCost = dto.ManaCost,
            Cmc = dto.Cmc,
            TypeLine = dto.TypeLine,
            OracleText = dto.OracleText,
            Colors = dto.Colors,
            ColorIdentity = dto.ColorIdentity,
            Keywords = dto.Keywords,
            PauperLegality = dto.PauperLegality,
            Layout = dto.Layout
        }).ToList();

        dbContext.Cards.AddRange(cardsToInsert);
        await dbContext.SaveChangesAsync();
        Console.WriteLine($"-> {cardsToInsert.Count} cartas importadas.");
    }

    private async Task ImportRulingsAsync(string filePath)
    {
        // OTIMIZAÇÃO: Traz todos os OracleIds do banco para a memória em uma única consulta
        var validOracleIds = new HashSet<Guid>(dbContext.Cards.Select(c => c.OracleId));

        var rulingsToInsert = new List<Ruling>();

        foreach (var line in await File.ReadAllLinesAsync(filePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var dto = JsonSerializer.Deserialize<RulingJsonDto>(line);
            if (dto != null)
            {
                // Validação O(1) na memória (ultra rápido)
                if (validOracleIds.Contains(dto.OracleId))
                {
                    rulingsToInsert.Add(new Ruling
                    {
                        Id = Guid.NewGuid(),
                        CardOracleId = dto.OracleId,
                        Source = dto.Source,
                        PublishedAt = dto.PublishedAt,
                        Comment = dto.Comment
                    });
                }
            }
        }

        int batchSize = 5000;
        for (int i = 0; i < rulingsToInsert.Count; i += batchSize)
        {
            var batch = rulingsToInsert.Skip(i).Take(batchSize);
            dbContext.Rulings.AddRange(batch);
            await dbContext.SaveChangesAsync();
        }

        Console.WriteLine($"-> {rulingsToInsert.Count} rulings importados para cartas válidas.");
    }
}