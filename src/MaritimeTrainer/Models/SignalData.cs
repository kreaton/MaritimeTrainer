namespace MaritimeTrainer.Models;

/// <summary>International Code of Signals: letter flags, numeral pennants, Morse, phonetics.</summary>
public static class SignalData
{
    public sealed record Entry(
        string Symbol,          // "A" or "7"
        string Phonetic,        // ICAO/IMO spoken word, e.g. "Alfa", "Tree"
        string CodeWord,        // ICS pennant / ITU code word for numerals, otherwise the same as Phonetic
        string[] Alts,          // other accepted spellings when typing
        string? Meaning,        // single-flag meaning (letters only)
        string? Note = null);   // secondary meaning, shown on study screens

    public static readonly IReadOnlyList<Entry> Letters =
    [
        new("A", "Alfa", "Alfa", ["alpha"], "I have a diver down; keep well clear at slow speed."),
        new("B", "Bravo", "Bravo", [], "I am taking in, discharging or carrying dangerous goods."),
        new("C", "Charlie", "Charlie", [], "Yes (affirmative)."),
        new("D", "Delta", "Delta", [], "Keep clear of me; I am manoeuvring with difficulty."),
        new("E", "Echo", "Echo", [], "I am altering my course to starboard."),
        new("F", "Foxtrot", "Foxtrot", ["foxtrott"], "I am disabled; communicate with me."),
        new("G", "Golf", "Golf", [], "I require a pilot.", "Fishing vessels on the grounds: I am hauling nets."),
        new("H", "Hotel", "Hotel", [], "I have a pilot on board."),
        new("I", "India", "India", [], "I am altering my course to port."),
        new("J", "Juliett", "Juliett", ["juliet"], "I am on fire and have dangerous cargo on board: keep well clear of me.",
            "Also: I am leaking dangerous cargo."),
        new("K", "Kilo", "Kilo", [], "I wish to communicate with you."),
        new("L", "Lima", "Lima", [], "You should stop your vessel instantly."),
        new("M", "Mike", "Mike", [], "My vessel is stopped and making no way through the water."),
        new("N", "November", "November", [], "No (negative)."),
        new("O", "Oscar", "Oscar", [], "Man overboard."),
        new("P", "Papa", "Papa", [], "In harbour: all persons should report on board; the vessel is about to proceed to sea.",
            "At sea (fishing vessels): my nets have come fast upon an obstruction."),
        new("Q", "Quebec", "Quebec", [], "My vessel is healthy and I request free pratique."),
        new("R", "Romeo", "Romeo", [], "No assigned meaning in the current International Code of Signals.",
            "Before 1969: “The way is off my ship; you may feel your way past me.”"),
        new("S", "Sierra", "Sierra", [], "My engines are going astern (I am operating astern propulsion)."),
        new("T", "Tango", "Tango", [], "Keep clear of me; I am engaged in pair trawling."),
        new("U", "Uniform", "Uniform", [], "You are running into danger."),
        new("V", "Victor", "Victor", [], "I require assistance."),
        new("W", "Whiskey", "Whiskey", ["whisky"], "I require medical assistance."),
        new("X", "X-ray", "X-ray", ["xray", "x ray"], "Stop carrying out your intentions and watch for my signals."),
        new("Y", "Yankee", "Yankee", [], "I am dragging my anchor."),
        new("Z", "Zulu", "Zulu", [], "I require a tug.", "Fishing vessels on the grounds: I am shooting nets."),
    ];

    public static readonly IReadOnlyList<Entry> Numbers =
    [
        new("0", "Zero", "Nadazero", ["nadazero", "nada zero", "nought", "oh"], null),
        new("1", "One", "Unaone", ["unaone", "una one", "wun"], null),
        new("2", "Two", "Bissotwo", ["bissotwo", "bisso two", "too"], null),
        new("3", "Tree", "Terrathree", ["three", "terrathree", "terra three"], null),
        new("4", "Fower", "Kartefour", ["four", "fower", "kartefour", "karte four"], null),
        new("5", "Fife", "Pantafive", ["five", "pantafive", "panta five"], null),
        new("6", "Six", "Soxisix", ["soxisix", "soxi six"], null),
        new("7", "Seven", "Setteseven", ["setteseven", "sette seven"], null),
        new("8", "Eight", "Oktoeight", ["oktoeight", "okto eight"], null),
        new("9", "Niner", "Novenine", ["nine", "novenine", "nove nine"], null),
    ];

    public static IEnumerable<Entry> All => Letters.Concat(Numbers);

    public static Entry? Find(char c) => All.FirstOrDefault(e => e.Symbol[0] == char.ToUpperInvariant(c));

    /// <summary>Two-flag (and longer) signals from the International Code of Signals that every mariner should know.</summary>
    public static readonly (string Hoist, string Meaning)[] Combinations =
    [
        ("NC", "I am in distress and require immediate assistance."),
        ("CB", "I require immediate assistance."),
        ("AN", "I need a doctor."),
        ("AC", "I am abandoning my vessel."),
        ("IT", "I am on fire."),
        ("DV", "I am drifting."),
        ("UW", "I wish you a pleasant voyage."),
    ];

    public static List<QuizItem> BuildItems(string pool)
    {
        IEnumerable<Entry> src = pool switch
        {
            "letters" => Letters,
            "numbers" => Numbers,
            _ => All,
        };
        var list = new List<QuizItem>();
        foreach (var e in src)
        {
            var isLetter = char.IsLetter(e.Symbol[0]);
            var morse = Morse.For(e.Symbol[0])!;
            var item = new QuizItem
            {
                Id = "sig:" + e.Symbol,
                Deck = "signals",
                Label = e.Symbol + " – " + e.Phonetic,
                Group = isLetter ? "letters" : "numbers",
                PhoneticAlts = e.Alts.Concat(e.CodeWord == e.Phonetic ? [] : [e.CodeWord]).ToArray(),
            };
            item.With(Rep.Symbol, e.Symbol)
                .With(Rep.Phonetic, e.Phonetic)
                .With(Rep.Flag, e.Symbol)
                .With(Rep.MorseAudio, morse)
                .With(Rep.MorseVisual, morse);
            if (e.Meaning is not null) item.With(Rep.Meaning, e.Meaning);
            list.Add(item);
        }
        return list;
    }
}
