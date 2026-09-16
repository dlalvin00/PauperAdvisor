namespace PauperAdvisor.Api.Models;

public class MatchAnalysisRequest
{
    public IFormFile BoardScreenshot { get; set; } = null!;
    public string MainDeckList { get; set; } = null!;
    public string SideboardList { get; set; } = null!;
}