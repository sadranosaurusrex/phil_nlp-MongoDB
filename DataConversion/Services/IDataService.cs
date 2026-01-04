using DataConversion.Domain.Models;
using MongoDB.Bson;

namespace DataConversion.Services;

public interface IDataService
{
    Task<bool> InitializeDataAsync();
    Task RefreshDataAsync();
    Task<List<PhilosophicalText>> GetAllTextsAsync();
    Task<PhilosophicalText> GetTextByIdAsync(ObjectId id);
    Task<List<Sentence>> GetSentencesByTextIdAsync(ObjectId textId);
}