using DataConversion.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataConversion.Domain.Models;

namespace DataConversion.Pages;

public class TextsModel : PageModel
{
    private readonly IDataService _dataService;

    public TextsModel(IDataService dataService)
    {
        _dataService = dataService;
    }

    public List<PhilosophicalText> Texts { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        try
        {
            Texts = await _dataService.GetAllTextsAsync();
        }
        catch (Exception ex)
        {
            Message = $"Error loading data: {ex.Message}";
        }
    }

    public async Task<IActionResult> OnPostRefreshAsync()
    {
        try
        {
            Message = "Starting CSV import...";
            Console.WriteLine("Starting CSV refresh...");
            
            await _dataService.RefreshDataAsync();
            
            Console.WriteLine("CSV refresh completed successfully");
            Message = "Database refreshed successfully!";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during refresh: {ex.Message}");
            Message = $"Error refreshing data: {ex.Message}";
        }

        return RedirectToPage();
    }
}