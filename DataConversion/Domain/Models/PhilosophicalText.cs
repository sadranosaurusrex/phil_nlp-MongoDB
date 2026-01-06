using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataConversion.Domain.Models;

public class PhilosophicalText
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string School { get; set; } = string.Empty;
    public int OriginalPublicationDate { get; set; }
    public int CorpusEditionDate { get; set; }
    public int SentenceCount {  get; set; }

    public PhilosophicalText incrementSentenceCount()
    {
        SentenceCount++;
        return this;
    }
}
public class Sentence
{
    [BsonId]
    public ObjectId Id { get; set; }
    public ObjectId PtId { get; set; }
    public string SentenceSpacy { get; set; } = string.Empty;
    public string SentenceStr { get; set; } = string.Empty;
    public int SentenceLength { get; set; }
    public string SentenceLowered { get; set; } = string.Empty;
    public List<string> TokenizedText { get; set; } = new();
    public string LemmatizedStr { get; set; } = string.Empty;
}