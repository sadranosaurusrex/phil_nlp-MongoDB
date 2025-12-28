using DataConversion.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataConversion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TextsController : ControllerBase
{
    private readonly IDataService _dataService;

    public TextsController(IDataService dataService)
    {
        _dataService = dataService;
    }

    [HttpGet("GetTexts")]
    public async Task<IActionResult> GetTexts()
    {
        try
        {
            var texts = await _dataService.GetAllTextsAsync();
            return Ok(texts);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}