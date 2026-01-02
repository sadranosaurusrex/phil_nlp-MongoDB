using DataConversion.Domain.Models;
using MongoDB.Bson;

namespace DataConversion.Services;

public interface IMongoDbService
{
    Task<List<PhilosophicalText>> GetAllTextsAsync();
    Task<PhilosophicalText> GetTextByIdAsync(ObjectId id);
    Task<List<SentenceDocument>> GetSentencesByTextIdAsync(ObjectId textId);
    Task RefreshDataAsync(string csvPath);
    Task<bool> HasDataAsync();
}