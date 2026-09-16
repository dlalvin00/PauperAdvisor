using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PauperAdvisor.Data;

namespace PauperAdvisor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardsController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("{name}")]
    public async Task<IActionResult> GetCardByName(string name)
    {
        // Busca a carta por nome aproximado e já inclui os rulings (Eager Loading)
        var card = await dbContext.Cards
            .Include(c => c.Rulings)
            .FirstOrDefaultAsync(c => EF.Functions.Like(c.Name, $"%{name}%"));

        if (card == null)
            return NotFound(new { Message = "Carta não encontrada no dataset Pauper." });

        return Ok(card);
    }
}