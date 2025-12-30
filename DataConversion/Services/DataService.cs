using DataConversion.Domain.Models;
using DataConversion.Infrastructure;
using MongoDB.Bson;

namespace DataConversion.Services;

public class DataService : IDataService
{
    private readonly IMongoDbService _mongoDbService;
    private readonly IConfiguration _configuration;

    public DataService(IMongoDbService mongoDbService, IConfiguration configuration)
    {
        _mongoDbService = mongoDbService;
        _configuration = configuration;
    }

    public async Task<bool> InitializeDataAsync()
    {
        if (!await _mongoDbService.HasDataAsync())
        {
            await RefreshDataAsync();
            return true;
        }
        return false;
    }

    public async Task RefreshDataAsync()
    {
        var csvPath = "philosophy_data.csv";
        
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException($"CSV file not found: {csvPath}");
        }
        
        Console.WriteLine($"Processing CSV file: {csvPath}");
        var texts = CsvConverter.ConvertCsvToTexts(csvPath);
        Console.WriteLine($"Converted {texts.Count} texts with {texts.Sum(t => t.Sentences.Count)} sentences");
        
        await _mongoDbService.RefreshDataAsync(texts);
        Console.WriteLine("Data saved to MongoDB successfully");
    }

    public async Task<List<PhilosophicalText>> GetAllTextsAsync()
    {
        return await _mongoDbService.GetAllTextsAsync();
    }

    public async Task<PhilosophicalText> GetTextByIdAsync(ObjectId id)
    {
        return await _mongoDbService.GetTextByIdAsync(id);
    }

    public async Task<List<SentenceDocument>> GetSentencesByTextIdAsync(ObjectId textId)
    {
        return await _mongoDbService.GetSentencesByTextIdAsync(textId);
    }
}