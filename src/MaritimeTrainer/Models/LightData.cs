using System.Globalization;

namespace MaritimeTrainer.Models;

/// <summary>One step of a light's repeating cycle. Colour is null while the light is dark.</summary>
public readonly record struct Seg(double Seconds, string? Colour, double Level = 1.0);

public sealed record LightSpec(string Key, string Abbr, string Name, string Desc, IReadOnlyList<Seg> Cycle, bool InCharacteristicsDeck)
{
    public double Period => Cycle.Sum(s => s.Seconds);
}

public static class Lights
{
    public const string W = "#fff6c8", R = "#ff3b30", G = "#2ee06a", Y = "#ffd60a", Bu = "#3b82f6";

    /// <summary>Alternating on/off durations starting with ON. The last OFF is stretched so the cycle equals <paramref name="period"/>.</summary>
    private static List<Seg> Pat(string colour, double period, params double[] d)
    {
        var list = new List<Seg>();
        double sum = 0;
        for (var i = 0; i < d.Length; i++)
        {
            list.Add(new Seg(d[i], i % 2 == 0 ? colour : null));
            sum += d[i];
        }
        var rest = Math.Round(period - sum, 3);
        if (rest > 0.0005)
        {
            if (d.Length % 2 == 1) list.Add(new Seg(rest, null));
            else list[^1] = new Seg(list[^1].Seconds + rest, null);
        }
        return list;
    }

    private static double[] Quick(int n, double on, double off, double lastOffOverride = -1)
    {
        var l = new List<double>();
        for (var i = 0; i < n; i++)
        {
            l.Add(on);
            if (i < n - 1) l.Add(off);
        }
        if (lastOffOverride > 0) l.Add(lastOffOverride);
        return l.ToArray();
    }

    private static LightSpec S(string key, string abbr, string name, string desc, IReadOnlyList<Seg> cycle, bool deck = false) =>
        new(key, abbr, name, desc, cycle, deck);

    public static readonly IReadOnlyList<LightSpec> All = Build();

    private static List<LightSpec> Build()
    {
        var l = new List<LightSpec>
        {
            S("F", "F W", "Fixed", "A continuous, steady light.", [new Seg(1, W)], true),
            S("Oc", "Oc W 4s", "Occulting", "The light is on for longer than it is dark; short, regular eclipses interrupt it.", Pat(W, 4, 3, 1), true),
            S("Oc2", "Oc(2) W 8s", "Group occulting", "Occulting, with a regular group of two eclipses in every period.", Pat(W, 8, 2.5, 1, 2.5, 2), true),
            S("Iso", "Iso W 4s", "Isophase", "Light and dark last exactly the same time.", Pat(W, 4, 2, 2), true),
            S("Fl", "Fl W 5s", "Flashing", "The light is shorter than the dark; fewer than 30 flashes a minute.", Pat(W, 5, 1, 4), true),
            S("Fl2", "Fl(2) W 5s", "Group flashing", "Flashes are shown in regular groups, here two flashes per period.", Pat(W, 5, 0.5, 0.8, 0.5), true),
            S("LFl", "LFl W 10s", "Long flashing", "A single flash of at least two seconds.", Pat(W, 10, 2, 8), true),
            S("Q", "Q W", "Quick flashing", "Continuous flashing at 50–60 flashes a minute.", Pat(W, 1, 0.3, 0.7), true),
            S("Q3", "Q(3) W 10s", "Group quick flashing", "Groups of three quick flashes followed by a long dark interval.", Pat(W, 10, Quick(3, 0.3, 0.7)), true),
            S("VQ", "VQ W", "Very quick flashing", "Continuous flashing at 100–120 flashes a minute.", Pat(W, 0.5, 0.15, 0.35), true),
            S("VQ3", "VQ(3) W 5s", "Group very quick flashing", "Groups of three very quick flashes followed by a dark interval.", Pat(W, 5, Quick(3, 0.15, 0.35)), true),
            S("UQ", "UQ W", "Ultra quick flashing", "Continuous, extremely fast flashing – at least 160 flashes a minute.", Pat(W, 0.25, 0.08, 0.17), true),
            S("MoA", "Mo(A) W 6s", "Morse code", "Light and dark are grouped to form a Morse letter – here A (dot-dash).", Pat(W, 6, 0.5, 0.5, 1.5), true),
            S("Fl21", "Fl(2+1) R 10s", "Composite group flashing", "Groups made of different numbers of flashes – here two, then one.", Pat(R, 10, 0.5, 0.8, 0.5, 1.8, 0.5), true),
            S("FFl", "F Fl W 8s", "Fixed and flashing", "A steady light that is made brighter by a flash at regular intervals.",
                [new Seg(3, W, 0.45), new Seg(1, W, 1), new Seg(4, W, 0.45)], true),
            S("AlWR", "Al WR 10s", "Alternating", "The colour changes in a regular sequence – here white and red alternate.",
                [new Seg(1.5, W), new Seg(3.5, null), new Seg(1.5, R), new Seg(3.5, null)], true),
            S("Q6LFl", "Q(6)+LFl W 15s", "Group quick + long flash", "Six quick flashes followed by one long flash.",
                Pat(W, 15, [.. Quick(6, 0.3, 0.7, 1.5), 2.0]), true),

            // Lights used by buoys – not in the characteristics deck (variants of the ones above).
            S("Q9", "Q(9) W 15s", "Group quick flashing (9)", "Nine quick flashes then a dark interval.", Pat(W, 15, Quick(9, 0.3, 0.7))),
            S("Fl2b", "Fl(2) W 5s", "Group flashing (isolated danger)", "Two flashes per period.", Pat(W, 5, 0.5, 0.8, 0.5)),
            S("Fl21G", "Fl(2+1) G 10s", "Composite group flashing (green)", "Two flashes then one, green.", Pat(G, 10, 0.5, 0.8, 0.5, 1.8, 0.5)),
            S("FlR", "Fl R 5s", "Flashing red", "Red flashing – any rhythm may be used on a lateral mark except Fl(2+1).", Pat(R, 5, 1, 4)),
            S("FlG", "Fl G 5s", "Flashing green", "Green flashing – any rhythm may be used on a lateral mark except Fl(2+1).", Pat(G, 5, 1, 4)),
            S("FlY", "Fl Y 5s", "Flashing yellow", "Yellow – any rhythm that is not used by a white light on a cardinal, isolated-danger or safe-water mark.", Pat(Y, 5, 1, 4)),
            S("AlBuY", "Al Bu Y 3s", "Alternating blue / yellow", "Blue and yellow flashes alternate (emergency wreck marking).",
                [new Seg(1, Bu), new Seg(0.5, null), new Seg(1, Y), new Seg(0.5, null)]),
        };
        return l;
    }

    public static LightSpec Get(string key) => All.First(s => s.Key == key);
    public static LightSpec? Find(string? key) => key is null ? null : All.FirstOrDefault(s => s.Key == key);

    /// <summary>Parses the period in seconds out of an abbreviation such as "Fl(2) W 5s"; null for continuous/short-period lights.</summary>
    public static double? ParsePeriod(string abbr)
    {
        foreach (var part in abbr.Split(' '))
            if (part.EndsWith('s') && double.TryParse(part[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return v;
        return null;
    }

    public static List<QuizItem> BuildItems() =>
        All.Where(s => s.InCharacteristicsDeck).Select(s => new QuizItem
        {
            Id = "light:" + s.Key,
            Deck = "lights",
            Label = s.Abbr + " – " + s.Name,
            Group = "lights",
        }
        .With(Rep.LightAnim, s.Key)
        .With(Rep.LightAbbr, s.Abbr)
        .With(Rep.LightName, s.Name)
        .With(Rep.LightDesc, s.Desc)).ToList();

    /// <summary>Colour codes used on charts.</summary>
    public static readonly (string Code, string Colour)[] ColourCodes =
    [
        ("W", "White"), ("R", "Red"), ("G", "Green"), ("Y", "Yellow"), ("Bu", "Blue"), ("Vi", "Violet"), ("Or", "Orange"),
    ];
}
