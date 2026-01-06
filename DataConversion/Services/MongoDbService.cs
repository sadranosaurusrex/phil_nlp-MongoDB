using DataConversion.Domain.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.Json;

namespace DataConversion.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoCollection<PhilosophicalText> _textsCollection;
    private readonly IMongoCollection<Sentence> _sentencesCollection;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017";
        var databaseName = "PhilosophyDb";
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _textsCollection = database.GetCollection<PhilosophicalText>("philosophical_texts");
        _sentencesCollection = database.GetCollection<Sentence>("sentences");
    }

    public async Task<List<PhilosophicalText>> GetAllTextsAsync()
    {
        return await _textsCollection.Find(_ => true).ToListAsync();
    }

    public async Task<PhilosophicalText> GetTextByIdAsync(ObjectId id)
    {
        return await _textsCollection.Find(t => t.Id == id).FirstAsync();
    }

    public async Task<List<Sentence>> GetSentencesByTextIdAsync(ObjectId textId)
    {
        return await _sentencesCollection.Find(s => s.PtId == textId).ToListAsync();
    }

    public async Task RefreshDataAsync(string cvsPath)
    {
        await _textsCollection.DeleteManyAsync(_ => true);
        await _sentencesCollection.DeleteManyAsync(_ => true);

        var textGroups = new Dictionary<string, PhilosophicalText>();
        var sentences = new List<Sentence>();
        var lines = File.ReadLines(cvsPath).Skip(1).ToList();
        var batchSize = 3500;

        foreach (var line in lines)
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 11) continue;

            var key = $"{parts[0]}{parts[1]}{parts[2]}";
            var originalDate = int.Parse(parts[5]);
            var corpusDate = int.Parse(parts[6]);

            if (!textGroups.ContainsKey(key)) {
                textGroups.Add(key, new PhilosophicalText
                {
                    Id = ObjectId.GenerateNewId(),
                    Title = parts[0],
                    Author = parts[1],
                    School = parts[2],
                    OriginalPublicationDate = originalDate,
                    CorpusEditionDate = corpusDate
                });
            }

            var sentenceLength = int.Parse(parts[7]);

            sentences.Add(new Sentence
            {
                Id = ObjectId.GenerateNewId(),
                PtId = textGroups[key].incrementSentenceCount().Id,
                SentenceSpacy = parts[3],
                SentenceStr = parts[4],
                SentenceLength = sentenceLength,
                SentenceLowered = parts[8],
                LemmatizedStr = parts[10],
                TokenizedText = parts[9].Replace("'", "").Replace("[", "").Replace("]", "").Split(", ").ToList()
            });

            if (sentences.Count > batchSize)
            {
                _sentencesCollection.InsertMany(sentences);
                sentences.Clear();
            }
        }

        await _textsCollection.InsertManyAsync(textGroups.Values);
        await _sentencesCollection.InsertManyAsync(sentences);
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