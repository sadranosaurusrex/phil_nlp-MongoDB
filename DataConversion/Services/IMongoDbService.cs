using DataConversion.Domain.Models;

namespace DataConversion.Services;

public interface IMongoDbService
{
    Task<List<PhilosophicalText>> GetAllTextsAsync();
    Task RefreshDataAsync(List<PhilosophicalText> texts);
    Task<bool> HasDataAsync();
}