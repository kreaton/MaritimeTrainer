using System.Text.Json;
using Microsoft.JSInterop;

namespace MaritimeTrainer.Services;

public sealed class ItemStat
{
    public int Attempts { get; set; }
    public int Correct { get; set; }
    /// <summary>Consecutive correct answers (reset by a mistake).</summary>
    public int Streak { get; set; }
    public long Last { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public double Accuracy => Attempts == 0 ? 0 : (double)Correct / Attempts;
}

public enum Mastery { Unseen, Weak, Learning, Good, Mastered }

/// <summary>Per-item learning statistics, stored in the browser's localStorage.</summary>
public sealed class ProgressService(IJSRuntime js)
{
    private const string Key = "mt.progress.v1";
    private Dictionary<string, ItemStat> _stats = new();
    private bool _loaded;

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
                _stats = JsonSerializer.Deserialize(json, AppJson.Default.DictionaryStringItemStat) ?? new();
        }
        catch
        {
            _stats = new();
        }
    }

    public ItemStat Get(string id) => _stats.TryGetValue(id, out var s) ? s : new ItemStat();

    public async Task RecordAsync(string id, bool correct)
    {
        if (!_stats.TryGetValue(id, out var s)) _stats[id] = s = new ItemStat();
        s.Attempts++;
        if (correct) { s.Correct++; s.Streak++; } else s.Streak = 0;
        s.Last = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(_stats, AppJson.Default.DictionaryStringItemStat));
        }
        catch
        {
            // ignore – progress then lasts for this visit only
        }
        Changed?.Invoke();
    }

    public async Task ResetAsync()
    {
        _stats = new();
        await SaveAsync();
    }

    public int TotalAttempts => _stats.Values.Sum(s => s.Attempts);
    public int TotalCorrect => _stats.Values.Sum(s => s.Correct);
    public double Accuracy => TotalAttempts == 0 ? 0 : (double)TotalCorrect / TotalAttempts;

    public static Mastery LevelOf(ItemStat s)
    {
        if (s.Attempts == 0) return Mastery.Unseen;
        var acc = s.Accuracy;
        if (s.Attempts >= 4 && acc >= 0.9 && s.Streak >= 3) return Mastery.Mastered;
        if (acc >= 0.8 && s.Streak >= 2) return Mastery.Good;
        if (acc >= 0.5) return Mastery.Learning;
        return Mastery.Weak;
    }

    public Mastery LevelOf(string id) => LevelOf(Get(id));
}
