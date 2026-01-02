using DataConversion.Domain.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DataConversion.Services;

public class MongoDbService : IMongoDbService
{
    public readonly IMongoCollection<PhilosophicalText> _textsCollection;
    public readonly IMongoCollection<SentenceDocument> _sentencesCollection;

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
        return await _textsCollection.Find(t => t.PtId == id).FirstOrDefaultAsync();
    }

    public async Task<List<SentenceDocument>> GetSentencesByTextIdAsync(ObjectId textId)
    {
        return await _sentencesCollection.Find(s => s.PtId == textId).ToListAsync();
    }

    public async Task RefreshDataAsync(string csvPath)
    {
        // Clear existing data
        await _textsCollection.DeleteManyAsync(_ => true);
        await _sentencesCollection.DeleteManyAsync(_ => true);

        var textGroups = new ConcurrentDictionary<string, PhilosophicalText>();
        var sentenceDocuments = new List<SentenceDocument>();
        var lines = File.ReadAllLines(csvPath).Skip(1);

        Parallel.ForEach(lines, line =>
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 11) return;

            if (!int.TryParse(parts[5], out int originalDate) || !int.TryParse(parts[6], out int corpusDate))
                return;

            var key = $"{parts[0]}_{parts[1]}_{parts[2]}";
            if (!textGroups.ContainsKey(key))
            {
                var text = textGroups.GetOrAdd(key, new PhilosophicalText
                {
                    Title = parts[0],
                    Author = parts[1],
                    School = parts[2],
                    OriginalPublicationDate = originalDate,
                    CorpusEditionDate = corpusDate
                });
                _textsCollection.InsertOne(text);
            }

            if (!int.TryParse(parts[7], out int sentenceLength)) return;

                List<string> tokenizedText = new List<string>();
            try
            {
                tokenizedText = JsonSerializer.Deserialize<List<string>>(parts[9].Replace("'", "\""));
            }
            catch
            {
                tokenizedText = new List<string>();
            }

            sentenceDocuments.Add(new SentenceDocument
            {
                PtId = textGroups[key].PtId,
                SentenceSpacy = parts[3] ?? string.Empty,
                SentenceStr = parts[4] ?? string.Empty,
                SentenceLength = sentenceLength,
                SentenceLowered = parts[8] ?? string.Empty,
                LemmatizedStr = parts[10] ?? string.Empty,
                TokenizedText = tokenizedText,
                Key = key,
            });
            _sentencesCollection.InsertOne(sentenceDocuments.Last());
        });
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else current += line[i];
        }
        result.Add(current);
        return result.ToArray();
    }

    public async Task<bool> HasDataAsync()
    {
        return await _textsCollection.CountDocumentsAsync(_ => true) > 0;
    }
}