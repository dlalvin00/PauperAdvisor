namespace PauperAdvisor.RAG.Services;

public class IngestionStatusService
{
    public bool IsRunning { get; set; }
    public int TotalCards { get; set; }
    public int ProcessedCards { get; set; }
    public string Message { get; set; } = "Aguardando inicialização.";

    // Calcula a porcentagem automaticamente
    public double Percentage => TotalCards == 0 ? 0 : Math.Round((double)ProcessedCards / TotalCards * 100, 2);
}