using Microsoft.JSInterop;
using MaritimeTrainer.Models;

namespace MaritimeTrainer.Services;

/// <summary>Plays Morse and feedback sounds through the Web Audio API (see wwwroot/js/trainer.js).</summary>
public sealed class AudioService(IJSRuntime js, SettingsService settings)
{
    private string? _playing;

    public event Action? StateChanged;

    public bool IsPlaying(string pattern) => _playing == pattern;

    /// <summary>Plays a Morse pattern ('.' '-' ' ' letter gap, '/' word gap); completes when the sound has finished or been stopped.</summary>
    public async Task PlayMorseAsync(string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return;
        await settings.EnsureLoadedAsync();
        var s = settings.Current;
        _playing = pattern;
        StateChanged?.Invoke();
        try
        {
            await js.InvokeVoidAsync("trainer.playMorse", pattern, s.CharWpm, s.EffWpm, s.ToneHz, s.Volume);
        }
        catch
        {
            // audio unavailable – nothing to do
        }
        finally
        {
            if (_playing == pattern)
            {
                _playing = null;
                StateChanged?.Invoke();
            }
        }
    }

    public async Task StopAsync()
    {
        try { await js.InvokeVoidAsync("trainer.stop"); } catch { }
    }

    /// <summary>A single key-down tone for the Morse keyer.</summary>
    public async Task BeepAsync(bool dah)
    {
        await settings.EnsureLoadedAsync();
        var s = settings.Current;
        var unit = 1200.0 / s.CharWpm;
        try { await js.InvokeVoidAsync("trainer.tone", s.ToneHz, (dah ? 3 : 1) * unit, s.Volume); } catch { }
    }

    public async Task ChimeAsync(bool correct)
    {
        await settings.EnsureLoadedAsync();
        if (!settings.Current.Effects) return;
        try { await js.InvokeVoidAsync("trainer.chime", correct, settings.Current.Volume); } catch { }
    }
}
