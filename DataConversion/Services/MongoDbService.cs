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

        // Create mapping of text key to ObjectId for fast lookup
        var textIdMap = textsToInsert
            .Select((text, index) => new { Key = $"{texts[index].Title}_{texts[index].Author}_{texts[index].School}", Id = text.Id })
            .ToDictionary(x => x.Key, x => x.Id);

        // Collect ALL sentences at once for bulk insert
        var allSentences = new List<SentenceDocument>();
        const int maxBatchSize = 50000; // Larger batches for better performance

        foreach (var text in texts)
        {
            var textKey = $"{text.Title}_{text.Author}_{text.School}";
            if (!textIdMap.TryGetValue(textKey, out var textId)) continue;

            var sentences = text.Sentences ?? new List<Sentence>();
            foreach (var sentence in sentences)
            {
                allSentences.Add(new SentenceDocument
                {
                    TextId = textId,
                    SentenceSpacy = sentence.SentenceSpacy ?? string.Empty,
                    SentenceStr = sentence.SentenceStr ?? string.Empty,
                    SentenceLength = sentence.SentenceLength,
                    SentenceLowered = sentence.SentenceLowered ?? string.Empty,
                    TokenizedTxt = sentence.TokenizedTxt ?? new List<string>(),
                    LemmatizedStr = sentence.LemmatizedStr ?? string.Empty
                });
            }

            // Insert in large batches to reduce MongoDB round trips
            if (allSentences.Count >= maxBatchSize)
            {
                await _sentencesCollection.InsertManyAsync(allSentences);
                allSentences.Clear();
            }
        }

        // Insert any remaining sentences
        if (allSentences.Any())
        {
            await _sentencesCollection.InsertManyAsync(allSentences);
        }
    }

    public async Task<bool> HasDataAsync()
    {
        return await _textsCollection.CountDocumentsAsync(_ => true) > 0;
    }
}