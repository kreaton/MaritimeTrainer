namespace MaritimeTrainer.Models;

public sealed record Preset(string Title, Rep? From, Rep? To);

public sealed record DeckInfo(
    string Id,
    string Title,
    string Icon,
    string Blurb,
    Rep[] Reps,
    (string Id, string Title)[] Pools,
    Preset[] Presets,
    string StudyPath);

public static class Decks
{
    public static readonly DeckInfo Signals = new(
        "signals", "Signal flags, Morse & phonetics", "🏴",
        "Letter flags, numeral pennants, Morse code, the spelling alphabet and what single flags mean.",
        [Rep.Symbol, Rep.Phonetic, Rep.Flag, Rep.MorseAudio, Rep.MorseVisual, Rep.Meaning],
        [("all", "Letters & numbers"), ("letters", "Letters only"), ("numbers", "Numbers only")],
        [
            new("Hear Morse → Letter", Rep.MorseAudio, Rep.Symbol),
            new("Letter → Morse", Rep.Symbol, Rep.MorseVisual),
            new("Flag → Letter", Rep.Flag, Rep.Symbol),
            new("Letter → Flag", Rep.Symbol, Rep.Flag),
            new("Flag → Phonetic", Rep.Flag, Rep.Phonetic),
            new("Phonetic → Flag", Rep.Phonetic, Rep.Flag),
            new("Hear Morse → Flag", Rep.MorseAudio, Rep.Flag),
            new("Flag → Morse", Rep.Flag, Rep.MorseVisual),
            new("Flag → Meaning", Rep.Flag, Rep.Meaning),
            new("Meaning → Flag", Rep.Meaning, Rep.Flag),
            new("Phonetic → Letter", Rep.Phonetic, Rep.Symbol),
            new("Everything mixed", null, null),
        ],
        "signals");

    public static readonly DeckInfo Buoys = new(
        "buoys", "IALA buoyage", "🛟",
        "Lateral, cardinal, isolated danger, safe water, special and wreck marks – shapes, colours, topmarks and how to pass them.",
        [Rep.MarkImage, Rep.MarkName, Rep.MarkPassing, Rep.Topmark, Rep.MarkColours, Rep.LightAnim, Rep.LightAbbr],
        [("all", "All marks"), ("lateral", "Lateral marks"), ("cardinal", "Cardinal marks"), ("other", "Danger, safe water, special & wreck")],
        [
            new("Picture → Name", Rep.MarkImage, Rep.MarkName),
            new("Name → Picture", Rep.MarkName, Rep.MarkImage),
            new("Picture → How to pass", Rep.MarkImage, Rep.MarkPassing),
            new("Topmark → Name", Rep.Topmark, Rep.MarkName),
            new("Colours → Name", Rep.MarkColours, Rep.MarkName),
            new("Light → Mark", Rep.LightAnim, Rep.MarkName),
            new("Mark → Light", Rep.MarkName, Rep.LightAbbr),
            new("Everything mixed", null, null),
        ],
        "buoyage");

    public static readonly DeckInfo Lights = new(
        "lights", "Light characteristics", "💡",
        "Recognise and decode flashing, occulting, isophase, quick and Morse lights – and read chart abbreviations.",
        [Rep.LightAnim, Rep.LightAbbr, Rep.LightName, Rep.LightDesc],
        [("all", "All characteristics")],
        [
            new("Watch → Type", Rep.LightAnim, Rep.LightName),
            new("Watch → Abbreviation", Rep.LightAnim, Rep.LightAbbr),
            new("Abbreviation → Type", Rep.LightAbbr, Rep.LightName),
            new("Abbreviation → Watch", Rep.LightAbbr, Rep.LightAnim),
            new("Type → Description", Rep.LightName, Rep.LightDesc),
            new("Description → Type", Rep.LightDesc, Rep.LightName),
            new("Everything mixed", null, null),
        ],
        "lights");

    public static readonly IReadOnlyList<DeckInfo> All = [Signals, Buoys, Lights];

    public static DeckInfo? Find(string id) => All.FirstOrDefault(d => d.Id == id);

    public static List<QuizItem> Items(string deckId, string pool, Region region) => deckId switch
    {
        "signals" => SignalData.BuildItems(pool),
        "buoys" => Marks.BuildItems(pool, region),
        "lights" => MaritimeTrainer.Models.Lights.BuildItems(),
        _ => [],
    };
}
