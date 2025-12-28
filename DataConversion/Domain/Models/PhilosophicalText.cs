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
    public int SentenceCount { get; set; } // Store count instead of full sentences
    
    // Keep for CSV converter compatibility - not stored in MongoDB
    [BsonIgnore]
    public List<Sentence> Sentences { get; set; } = new();
}

public class SentenceDocument
{
    [BsonId]
    public ObjectId Id { get; set; }
    public ObjectId TextId { get; set; } // Reference to PhilosophicalText
    public string SentenceSpacy { get; set; } = string.Empty;
    public string SentenceStr { get; set; } = string.Empty;
    public int SentenceLength { get; set; }
    public string SentenceLowered { get; set; } = string.Empty;
    public List<string> TokenizedTxt { get; set; } = new();
    public string LemmatizedStr { get; set; } = string.Empty;
}

// Keep original for compatibility
public class Sentence
{
    public string SentenceSpacy { get; set; } = string.Empty;
    public string SentenceStr { get; set; } = string.Empty;
    public int SentenceLength { get; set; }
    public string SentenceLowered { get; set; } = string.Empty;
    public List<string> TokenizedTxt { get; set; } = new();
    public string LemmatizedStr { get; set; } = string.Empty;
}