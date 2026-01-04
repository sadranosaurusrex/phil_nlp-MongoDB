using DataConversion.Domain.Models;
using System.Text.Json;

namespace DataConversion.Infrastructure;

public class CsvConverter
{
    public static List<PhilosophicalText> ConvertCsvToTexts(string csvPath)
    {
        var lines = File.ReadAllLines(csvPath).Skip(1); // Skip header
        var textGroups = new Dictionary<string, PhilosophicalText>();

        foreach (var line in lines)
        {
            var parts = ParseCsvLine(line);
            if (parts.Length < 11) continue; // CSV has 11 columns, not 12

            var key = $"{parts[0]}_{parts[1]}_{parts[2]}"; // title_author_school
            
            if (!textGroups.ContainsKey(key))
            {
                if (!int.TryParse(parts[5], out int originalDate) || !int.TryParse(parts[6], out int corpusDate))
                    continue; // Skip if dates can't be parsed
                    
                textGroups[key] = new PhilosophicalText
                {
                    Title = parts[0],
                    Author = parts[1],
                    School = parts[2],
                    OriginalPublicationDate = originalDate,
                    CorpusEditionDate = corpusDate
                };
            }

            if (!int.TryParse(parts[7], out int sentenceLength))
                continue; // Skip if sentence length can't be parsed

            List<string> tokenizedText = null;
            if (!string.IsNullOrEmpty(parts[9]))
            {
                try
                {
                    tokenizedText = JsonSerializer.Deserialize<List<string>>(parts[9].Replace("'", "\""));
                }
                catch
                {
                    tokenizedText = new List<string>(); // Default to empty list if parsing fails
                }
            }
            else
            {
                tokenizedText = new List<string>();
            }
        }

        return textGroups.Values.ToList();
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
}