using DataConversion.Domain.Models;
using MongoDB.Driver;
using MongoDB.Bson;

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
        await Task.WhenAll(
            _textsCollection.DeleteManyAsync(_ => true),
            _sentencesCollection.DeleteManyAsync(_ => true)
        );

        var gate = new SemaphoreSlim(20);

        var textGroups = new Dictionary<string, PhilosophicalText>();
        var sentences = new List<Sentence>();
        //var lines = File.ReadLines(cvsPath).Skip(1).ToList();
        var batchSize = 15000;
        List<Task> tasks = new List<Task>();
        
        foreach (var line in File.ReadLines(cvsPath).Skip(1))
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
                //Id = ObjectId.GenerateNewId(),
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
                var batch = sentences;
                sentences = new List<Sentence>();

                await gate.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await _sentencesCollection.InsertManyAsync(batch);
                    }
                    finally
                    {
                        gate.Release();
                        Console.WriteLine($"inserting {batchSize} lines...");
                    }
                }));
            }
        }

        tasks.Add(_textsCollection.InsertManyAsync(textGroups.Values));
        tasks.Add(_sentencesCollection.InsertManyAsync(sentences));
        await Task.WhenAll(tasks);
        gate.Dispose();
    }

    private static string[] ParseCsvLine(string line)
    {
        ReadOnlySpan<char> span = line.AsSpan();
        var result = new List<string>();
        var current = 0;
        var inQuotes = false;

        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == '"') inQuotes = !inQuotes;
            else if (span[i] == ',' && !inQuotes)
            {
                result.Add(span.Slice(current, i -current).ToString());
                current = i +1;
            }
        }
        result.Add(span.Slice(current).ToString());
        return result.ToArray();
    }
    public async Task<bool> HasDataAsync()
    {
        return await _textsCollection.CountDocumentsAsync(_ => true) > 0;
    }
}