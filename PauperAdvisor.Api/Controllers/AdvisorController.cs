using Microsoft.AspNetCore.Mvc;
using PauperAdvisor.Api.Models;
using PauperAdvisor.Data.DeckLists;
using PauperAdvisor.Domain.Decks;
using PauperAdvisor.RAG.Services;

namespace PauperAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdvisorController(ChatService chatService, DeckListParser deckListParser) : ControllerBase
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
    public async Task<IActionResult> AnalyzeMatch([FromForm] MatchAnalysisRequest request, CancellationToken cancellationToken)
    {
        if (request.BoardScreenshot == null || request.BoardScreenshot.Length == 0)
            return BadRequest("A imagem do print é obrigatória.");

        DeckList? mainDeck = null;
        DeckList? sideBoard = null;

        var deckErrors = new Dictionary<string, IReadOnlyList<string>>();

        try
        {
            mainDeck = await deckListParser.ParseAndValidateAsync(request.MainDeckList, cancellationToken);
        }
        catch (DeckListValidationException ex)
        {
            deckErrors["mainDeckList"] = ex.Errors;
        }

        try
        {
            sideBoard = await deckListParser.ParseAndValidateAsync(request.SideboardList, cancellationToken);
        }
        catch (DeckListValidationException ex)
        {
            deckErrors["sideBoardList"] = ex.Errors;
        }

        if(deckErrors.Count > 0)
        {
            return BadRequest(new
            {
                Message = "Uma ou mais decklists são Inválidas.",
                Errors = deckErrors
            });
        }

        using var stream = request.BoardScreenshot.OpenReadStream();

        var analysis = await chatService.AnalyzeMatchupAsync(
            stream,
            mainDeck!,
            sideBoard!,
            "What deck is the opponent playing and what should I side in and out?"
        );

        return Ok(new { Analysis = analysis });
    }
}