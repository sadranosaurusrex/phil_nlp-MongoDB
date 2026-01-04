using DataConversion.Domain.Models;
using MongoDB.Bson;

namespace DataConversion.Services;

public class DataService : IDataService
{
    private readonly IMongoDbService _mongoDbService;

    public DataService(IMongoDbService mongoDbService)
    {
        _mongoDbService = mongoDbService;
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
        
        await _mongoDbService.RefreshDataAsync(csvPath); //The main operation

        Console.WriteLine("Data saved to MongoDB successfully.");
    }

    public async Task<List<PhilosophicalText>> GetAllTextsAsync()
    {
        return await _mongoDbService.GetAllTextsAsync();
    }

    public async Task<PhilosophicalText> GetTextByIdAsync(ObjectId id)
    {
        return await _mongoDbService.GetTextByIdAsync(id);
    }

    public async Task<List<Sentence>> GetSentencesByTextIdAsync(ObjectId textId)
    {
        return await _mongoDbService.GetSentencesByTextIdAsync(textId);
    }
}