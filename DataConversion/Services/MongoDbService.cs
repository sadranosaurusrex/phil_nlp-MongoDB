using DataConversion.Domain.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DataConversion.Services;

public class MongoDbService : IMongoDbService
{
    public readonly IMongoCollection<PhilosophicalText> _textsCollection;
    public readonly IMongoCollection<SentenceDocument> _sentencesCollection;
    public readonly IDataService _dataService;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var databaseName = "PhilosophyDb";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _textsCollection = database.GetCollection<PhilosophicalText>("philosophical_texts");
        _sentencesCollection = database.GetCollection<SentenceDocument>("sentences");
        var indexKeys = Builders<SentenceDocument>.IndexKeys.Ascending(s => s.PtId);
        _sentencesCollection.Indexes.CreateOne(new CreateIndexModel<SentenceDocument>(indexKeys));
    }

    public async Task<List<PhilosophicalText>> GetAllTextsAsync()
    {
        List<PhilosophicalText> text;
        if (await _textsCollection.Find(_ => true).ToListAsync() == null)
        {
            await _dataService.RefreshDataAsync();
        }
        text = await _textsCollection.Find(_ => true).ToListAsync();
        return text;
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
        await _textsCollection.DeleteManyAsync(_ => true);
        await _sentencesCollection.DeleteManyAsync(_ => true);

        var textGroups = new ConcurrentDictionary<string, PhilosophicalText>();
        var sentences = new List<SentenceDocument>();
        var lines = File.ReadLines(csvPath).Skip(1);
        int batchSize = 3500;

        Parallel.ForEach(lines, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, line =>
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 11) return;

            if (!int.TryParse(parts[5], out int originalDate) || !int.TryParse(parts[6], out int corpusDate))
                return;

            var key = $"{parts[0]}_{parts[1]}_{parts[2]}";

            var text = textGroups.GetOrAdd(key, k =>
            {
                var newText = new PhilosophicalText
                {
                    PtId = ObjectId.GenerateNewId(),
                    Title = parts[0],
                    Author = parts[1],
                    School = parts[2],
                    OriginalPublicationDate = originalDate,
                    CorpusEditionDate = corpusDate
                };
                return newText;
            });

            if (!int.TryParse(parts[7], out int sentenceLength)) return;

            List<string> tokenizedText;
            try
            {
                tokenizedText = JsonSerializer.Deserialize<List<string>>(parts[9].Replace("'", "\"")) ?? new List<string>();
            }
            catch
            {
                tokenizedText = new List<string>();
            }

            sentences.Add(new SentenceDocument
            {
                PtId = text.PtId,
                SentenceSpacy = parts[3] ?? string.Empty,
                SentenceStr = parts[4] ?? string.Empty,
                SentenceLength = sentenceLength,
                SentenceLowered = parts[8] ?? string.Empty,
                LemmatizedStr = parts[10] ?? string.Empty,
                TokenizedText = tokenizedText
            });
            text.IncrementSentenceCount();
        });
        _textsCollection.InsertMany(textGroups.Values);

        for (var i = 0; i < sentences.Count; i += batchSize)
        {
            var batch = sentences.Skip(i).Take(batchSize);
            _sentencesCollection.InsertMany(batch);
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuotes = !inQuotes;
            else if (line[i] == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(line[i]);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    public async Task<bool> HasDataAsync()
    {
        return await _textsCollection.CountDocumentsAsync(_ => true) > 0;
    }
}