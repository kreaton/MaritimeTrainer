using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MaritimeTrainer.Models;

namespace MaritimeTrainer.Services;

public sealed class AppSettings
{
    /// <summary>Speed each Morse character is sent at (words per minute).</summary>
    public int CharWpm { get; set; } = 18;
    /// <summary>Overall speed. Lower than CharWpm stretches the gaps between letters (Farnsworth timing).</summary>
    public int EffWpm { get; set; } = 10;
    public int ToneHz { get; set; } = 600;
    public double Volume { get; set; } = 0.6;
    public bool Effects { get; set; } = true;
    public bool AutoPlay { get; set; } = true;
    public Region Region { get; set; } = Region.A;
    /// <summary>The last light typed on the Lights page.</summary>
    public string CustomLight { get; set; } = "Fl(3) G 10s";
    /// <summary>The user's own sector light, as text ("R 245-260, W 260-265, G 265-280").</summary>
    public string CustomSectors { get; set; } = "";
    public string CustomSectorAbbr { get; set; } = "Oc WRG 6s";
    /// <summary>True when the user's sector bearings are given from seaward (as in light lists).</summary>
    public bool SectorsFromSeaward { get; set; } = true;
}

[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(Dictionary<string, ItemStat>))]
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
internal partial class AppJson : JsonSerializerContext;

public sealed class SettingsService(IJSRuntime js)
{
    private const string Key = "mt.settings.v1";
    private bool _loaded;

    public AppSettings Current { get; private set; } = new();
    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (!string.IsNullOrWhiteSpace(json))
                Current = JsonSerializer.Deserialize(json, AppJson.Default.AppSettings) ?? new AppSettings();
        }
        catch
        {
            Current = new AppSettings();
        }
        Normalize();
    }

    public async Task SaveAsync()
    {
        Normalize();
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(Current, AppJson.Default.AppSettings));
        }
        catch
        {
            // storage may be unavailable (private mode) – settings then last for this visit only
        }
        Changed?.Invoke();
    }

    private void Normalize()
    {
        Current.CharWpm = Math.Clamp(Current.CharWpm, 5, 30);
        Current.EffWpm = Math.Clamp(Current.EffWpm, 4, Current.CharWpm);
        Current.ToneHz = Math.Clamp(Current.ToneHz, 300, 1200);
        Current.Volume = Math.Clamp(Current.Volume, 0, 1);
    }
}
