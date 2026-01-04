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
        Texts = await _dataService.GetAllTextsAsync();
        if (Texts.Count == 0)
        {
            await _dataService.RefreshDataAsync();
            Texts = await _dataService.GetAllTextsAsync();
        }
    }

    public async Task<IActionResult> OnPostRefreshAsync()
    {
        try
        {
            Message = "Starting CSV import...";
            long start = DateTime.UtcNow.Ticks; ;
            Console.WriteLine($"Starting CSV refresh at {start}...");
            
            await _dataService.RefreshDataAsync();

            long end = DateTime.UtcNow.Ticks;
            double durationSeconds = (end - start) / (double)TimeSpan.TicksPerSecond;
            Console.WriteLine($"CSV refresh completed successfully at {end}\nThis process took {durationSeconds:F2} seconds.");
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