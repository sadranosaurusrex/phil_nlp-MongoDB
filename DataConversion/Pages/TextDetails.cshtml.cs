using DataConversion.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataConversion.Domain.Models;
using MongoDB.Bson;

namespace DataConversion.Pages;

public class TextDetailsModel : PageModel
{
    private readonly IDataService _dataService;

    public TextDetailsModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public PhilosophicalText Text { get; set; } = new();
    public List<SentenceDocument> Sentences { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out var objectId))
        {
            return NotFound();
        }

        try
        {
            Text = await _dataService.GetTextByIdAsync(objectId);
            if (Text == null)
            {
                return NotFound();
            }

            Sentences = await _dataService.GetSentencesByTextIdAsync(objectId);
        }
        catch (Exception ex)
        {
            Message = $"Error loading data: {ex.Message}";
        }

        return Page();
    }
}