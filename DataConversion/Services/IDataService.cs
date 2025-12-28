using DataConversion.Domain.Models;

namespace DataConversion.Services;

public interface IDataService
{
    Task<bool> InitializeDataAsync();
    Task RefreshDataAsync();
    Task<List<PhilosophicalText>> GetAllTextsAsync();
}