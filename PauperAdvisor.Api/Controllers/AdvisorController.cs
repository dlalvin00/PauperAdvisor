using Microsoft.AspNetCore.Mvc;
using PauperAdvisor.Api.Models;
using PauperAdvisor.RAG.Services;

namespace PauperAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvisorController(ChatService chatService) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return BadRequest("A pergunta não pode estar vazia.");

        // O processo pode levar alguns segundos dependendo da geração do LLM
        var answer = await chatService.AskAdvisorAsync(question);

        return Ok(new
        {
            Question = question,
            Answer = answer
        });
    }

    [HttpPost("analyze-match")]
    public async Task<IActionResult> AnalyzeMatch([FromForm] MatchAnalysisRequest request)
    {
        if (request.BoardScreenshot == null || request.BoardScreenshot.Length == 0)
            return BadRequest("A imagem do print é obrigatória.");

        using var stream = request.BoardScreenshot.OpenReadStream();

        var analysis = await chatService.AnalyzeMatchupAsync(
            stream,
            request.MainDeckList,
            request.SideboardList,
            "What deck is the opponent playing and what should I side in and out?"
        );

        return Ok(new { Analysis = analysis });
    }
}