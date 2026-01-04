using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataConversion.Domain.Models;
public class PhilosophicalText
{
    [BsonId]
    public ObjectId PtId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string School  { get; set; } = string.Empty;
    public int OriginalPublicationDate { get; set; }
    public int CorpusEditionDate { get; set; }
    private int _sentenceCount;
    public int SentenceCount { get => _sentenceCount; set => _sentenceCount = value; }

    public void IncrementSentenceCount ()
    {
        Interlocked.Increment(ref _sentenceCount);
    }
}

public class SentenceDocument
{
    [BsonId]
    public ObjectId SentenceId { get; set; }
    public ObjectId PtId { get; set; } // Reference to PhilosophicalText.PtId
    public String SentenceSpacy { get; set; } = string.Empty;
    public String SentenceStr {  get; set; } = string.Empty;
    public int SentenceLength { get; set; }
    public string SentenceLowered { get; set; } = string.Empty;
    public List<string> TokenizedText { get; set; } = new();
    public string LemmatizedStr { get; set; } = string.Empty;
}