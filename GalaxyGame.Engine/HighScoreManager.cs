using System.Text.Json;
using GalaxyGame.Engine.Models;

namespace GalaxyGame.Engine;

public class HighScoreManager
{
    private const int MaxEntries = 10;
    private readonly string _filePath;

    public HighScoreManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "GalaxyShooter");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "highscores.json");
    }

    public List<ScoreEntry> Load()
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<ScoreEntry>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public List<ScoreEntry> Add(string playerName, int score)
    {
        var entries = Load();
        entries.Add(new ScoreEntry { PlayerName = playerName, Score = score });
        entries = entries
            .OrderByDescending(e => e.Score)
            .Take(MaxEntries)
            .ToList();

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);

        return entries;
    }
}
