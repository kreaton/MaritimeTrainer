namespace MaritimeTrainer.Models;

/// <summary>
/// A "representation" is one way of presenting a fact: the flag for a letter, its Morse sound,
/// its phonetic word, the picture of a buoy, the animation of a light, and so on.
/// Every quiz question is "show representation X, pick the matching representation Y".
/// </summary>
public enum Rep
{
    // Signals deck
    Symbol, Phonetic, Flag, MorseAudio, MorseVisual, Meaning,
    // IALA buoyage deck
    MarkImage, MarkName, MarkPassing, Topmark, MarkColours,
    // Lights (used by both the lights deck and the buoyage deck)
    LightAnim, LightAbbr, LightName, LightDesc,
}

public enum Region { A, B }

public static class RepInfo
{
    public static string Title(Rep r) => r switch
    {
        Rep.Symbol => "Letter / number",
        Rep.Phonetic => "Phonetic word",
        Rep.Flag => "Flag",
        Rep.MorseAudio => "Morse (sound)",
        Rep.MorseVisual => "Morse (dots & dashes)",
        Rep.Meaning => "Single-flag meaning",
        Rep.MarkImage => "Buoy picture",
        Rep.MarkName => "Mark name",
        Rep.MarkPassing => "How to pass",
        Rep.Topmark => "Topmark",
        Rep.MarkColours => "Colours",
        Rep.LightAnim => "Light (watch it)",
        Rep.LightAbbr => "Chart abbreviation",
        Rep.LightName => "Light type",
        Rep.LightDesc => "Description",
        _ => r.ToString(),
    };

    public static string Short(Rep r) => r switch
    {
        Rep.Symbol => "Letter",
        Rep.Phonetic => "Phonetic",
        Rep.Flag => "Flag",
        Rep.MorseAudio => "Morse sound",
        Rep.MorseVisual => "Morse",
        Rep.Meaning => "Meaning",
        Rep.MarkImage => "Picture",
        Rep.MarkName => "Name",
        Rep.MarkPassing => "How to pass",
        Rep.Topmark => "Topmark",
        Rep.MarkColours => "Colours",
        Rep.LightAnim => "Light",
        Rep.LightAbbr => "Abbreviation",
        Rep.LightName => "Light type",
        Rep.LightDesc => "Description",
        _ => r.ToString(),
    };

    public static string Icon(Rep r) => r switch
    {
        Rep.Symbol => "Aa",
        Rep.Phonetic => "💬",
        Rep.Flag => "🏴",
        Rep.MorseAudio => "🔊",
        Rep.MorseVisual => "•–",
        Rep.Meaning => "📖",
        Rep.MarkImage => "🛟",
        Rep.MarkName => "🏷",
        Rep.MarkPassing => "🧭",
        Rep.Topmark => "▲",
        Rep.MarkColours => "🎨",
        Rep.LightAnim => "💡",
        Rep.LightAbbr => "Fl",
        Rep.LightName => "🏷",
        Rep.LightDesc => "📖",
        _ => "",
    };

    public static bool IsMorse(Rep r) => r is Rep.MorseAudio or Rep.MorseVisual;

    /// <summary>Answers that can be entered by keyboard (text) or by the dit/dah keyer.</summary>
    public static bool IsTypable(Rep r) => r is Rep.Symbol or Rep.Phonetic or Rep.MorseAudio or Rep.MorseVisual;

    /// <summary>Big visual representations are laid out larger and get a card of their own.</summary>
    public static bool IsVisual(Rep r) => r is Rep.Flag or Rep.MarkImage or Rep.Topmark or Rep.LightAnim;

    public static string PromptHint(Rep prompt, Rep answer) => answer switch
    {
        Rep.Symbol => "Which letter or number is it?",
        Rep.Phonetic => "Which phonetic word?",
        Rep.Flag => "Which flag?",
        Rep.MorseAudio or Rep.MorseVisual => "Which Morse code?",
        Rep.Meaning => "What does this flag mean on its own?",
        Rep.MarkImage => "Which buoy?",
        Rep.MarkName => "Which mark is it?",
        Rep.MarkPassing => "How should you pass it?",
        Rep.Topmark => "Which topmark?",
        Rep.MarkColours => "What are its colours?",
        Rep.LightAnim => "Which light?",
        Rep.LightAbbr => "Which chart abbreviation?",
        Rep.LightName => "What type of light?",
        Rep.LightDesc => "Which description fits?",
        _ => "Pick the match.",
    };
}
