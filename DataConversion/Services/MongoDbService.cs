using DataConversion.Domain.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DataConversion.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoCollection<PhilosophicalText> _textsCollection;
    private readonly IMongoCollection<SentenceDocument> _sentencesCollection;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var databaseName = "PhilosophyDb";
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _textsCollection = database.GetCollection<PhilosophicalText>("philosophical_texts");
        _sentencesCollection = database.GetCollection<SentenceDocument>("sentences");
    }

    public async Task<List<PhilosophicalText>> GetAllTextsAsync()
    {
        return await _textsCollection.Find(_ => true).ToListAsync();
    }

    public async Task<PhilosophicalText> GetTextByIdAsync(ObjectId id)
    {
        return await _textsCollection.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<SentenceDocument>> GetSentencesByTextIdAsync(ObjectId textId)
    {
        return await _sentencesCollection.Find(s => s.TextId == textId).ToListAsync();
    }

    public async Task RefreshDataAsync(List<PhilosophicalText> texts)
    {
        // Clear existing data
        await _textsCollection.DeleteManyAsync(_ => true);
        await _sentencesCollection.DeleteManyAsync(_ => true);
        
        if (!texts.Any()) return;

        // Convert to separate collections
        var textsToInsert = new List<PhilosophicalText>();
        var sentencesToInsert = new List<SentenceDocument>();

        foreach (var text in texts)
        {
            var textDoc = new PhilosophicalText
            {
                Title = text.Title,
                Author = text.Author,
                School = text.School,
                OriginalPublicationDate = text.OriginalPublicationDate,
                CorpusEditionDate = text.CorpusEditionDate,
                SentenceCount = text.Sentences?.Count ?? 0
            };
            
            textsToInsert.Add(textDoc);
        }

        // Insert texts first to get IDs
        await _textsCollection.InsertManyAsync(textsToInsert);

        // Now insert sentences with text references in batches
        const int batchSize = 1000;
        for (int i = 0; i < texts.Count; i++)
        {
            var textId = textsToInsert[i].Id;
            var sentences = texts[i].Sentences ?? new List<Sentence>();
            
            for (int j = 0; j < sentences.Count; j += batchSize)
            {
                var batch = sentences.Skip(j).Take(batchSize).Select(sentence => new SentenceDocument
                {
                    TextId = textId,
                    SentenceSpacy = sentence.SentenceSpacy ?? string.Empty,
                    SentenceStr = sentence.SentenceStr ?? string.Empty,
                    SentenceLength = sentence.SentenceLength,
                    SentenceLowered = sentence.SentenceLowered ?? string.Empty,
                    TokenizedTxt = sentence.TokenizedTxt ?? new List<string>(),
                    LemmatizedStr = sentence.LemmatizedStr ?? string.Empty
                }).ToList();
                
                if (batch.Any())
                {
                    await _sentencesCollection.InsertManyAsync(batch);
                }
            }
        }
    }

    public async Task<bool> HasDataAsync()
    {
        return await _textsCollection.CountDocumentsAsync(_ => true) > 0;
    }
}