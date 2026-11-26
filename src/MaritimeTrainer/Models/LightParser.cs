using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MaritimeTrainer.Models;

/// <summary>
/// Reads a chart light abbreviation typed by the user (for example "Fl(2) W 10s", "Q(6)+LFl W 15s",
/// "Oc(2+1) R 12s 15m 8M", "Mo(U) W 15s", "Al.Fl WR 6s", "Fl WRG 5s") and turns it into a light cycle
/// that can be animated and drawn as a time band.
/// Timings inside the period are not printed on charts, so typical values are used.
/// </summary>
public static partial class LightParser
{
    public enum Kind { F, Fl, LFl, Q, VQ, UQ, IQ, IVQ, IUQ, Oc, Iso, Mo, FFl }

    /// <summary>One characteristic, e.g. Fl(2+1) → Groups [2, 1]; Mo(A) → Letters "A".</summary>
    public sealed record Part(Kind Kind, IReadOnlyList<int> Groups, string? Letters);

    public sealed record Result(
        string Input,
        string Canonical,
        string Name,
        string Description,
        IReadOnlyList<Part> Parts,
        bool Alternating,
        IReadOnlyList<string> Colours,
        double Period,
        bool PeriodGiven,
        double? HeightM,
        double? RangeNm,
        IReadOnlyList<string> Notes)
    {
        /// <summary>True for a sector light: several colours without "Al" – the colour seen depends on the bearing.</summary>
        public bool IsSector => !Alternating && Colours.Count > 1;

        /// <summary>The animated cycle. For a sector light, <paramref name="colour"/> picks the sector to show.</summary>
        public LightSpec ToSpec(string? colour = null)
        {
            var cols = Alternating ? Colours : [colour is not null && Colours.Contains(colour) ? colour : Colours[0]];
            var cycle = BuildCycle(Parts, cols.Select(ColourHex).ToList(), Period);
            var key = "custom:" + Canonical + ":" + string.Join("", cols);
            return new LightSpec(key, Canonical, Name, Description, cycle, false);
        }

        /// <summary>
        /// The rhythm drawn in one given colour (a CSS colour), whatever the chart colours are –
        /// used to show a sector light's rhythm in the colour of the sector you are in.
        /// </summary>
        public LightSpec InColour(string hex)
        {
            var cycle = BuildCycle(Parts, [hex], Period);
            return new LightSpec("custom:" + Canonical + ":" + hex, Canonical, Name, Description, cycle, false);
        }
    }

    public static readonly (string Code, string Name, string Hex)[] ColourTable =
    [
        ("W", "white", Lights.W), ("R", "red", Lights.R), ("G", "green", Lights.G), ("Y", "yellow", Lights.Y),
        ("Bu", "blue", Lights.Bu), ("Vi", "violet", "#b98cff"), ("Or", "orange", "#ff9f1a"),
    ];

    public static string ColourName(string code) => ColourTable.FirstOrDefault(c => c.Code == code).Name ?? code;
    public static string ColourHex(string code) => ColourTable.FirstOrDefault(c => c.Code == code).Hex ?? Lights.W;

    private static string N(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ parsing

    [GeneratedRegex(@"(?<!\w)(\d+(?:\.\d+)?)\s*(?:[sS]|sec)(?![A-Za-z])")]
    private static partial Regex PeriodRx();

    [GeneratedRegex(@"(?<!\w)(\d+(?:\.\d+)?)\s*m(?![A-Za-z])")]
    private static partial Regex HeightRx();

    [GeneratedRegex(@"(?<!\w)(\d+(?:\.\d+)?)\s*M(?![A-Za-z])")]
    private static partial Regex RangeRx();

    private static readonly (string Token, Kind Kind)[] KindTokens =
    [
        ("FFl", Kind.FFl), ("IVQ", Kind.IVQ), ("IUQ", Kind.IUQ), ("IQ", Kind.IQ), ("LFl", Kind.LFl),
        ("VQ", Kind.VQ), ("UQ", Kind.UQ), ("Iso", Kind.Iso), ("Oc", Kind.Oc), ("Mo", Kind.Mo),
        ("Fl", Kind.Fl), ("Q", Kind.Q), ("F", Kind.F),
    ];

    public static bool TryParse(string? input, out Result? result, out string error)
    {
        result = null;
        error = "";
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Type a light, for example Fl(2) W 10s.";
            return false;
        }

        var notes = new List<string>();
        var s = input.Trim();
        s = Regex.Replace(s, @"(\d),(\d)", "$1.$2");                // 2,5s → 2.5s
        s = s.Replace('–', '-').Replace('·', '.');

        double? Take(Regex rx)
        {
            var m = rx.Match(s);
            if (!m.Success) return null;
            s = s.Remove(m.Index, m.Length).Insert(m.Index, " ");
            return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        var period = Take(PeriodRx());
        var height = Take(HeightRx());
        var range = Take(RangeRx());
        s = Regex.Replace(s, @"\.(?!\d)", " ");                    // chart style "Fl.R.5s"
        s = s.Replace(",", " ");

        var pos = 0;
        void Skip() { while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++; }
        bool At(string tok) => string.Compare(s, pos, tok, 0, tok.Length, StringComparison.OrdinalIgnoreCase) == 0;

        Skip();
        var alternating = false;
        if (At("Al") && !At("All"))
        {
            alternating = true;
            pos += 2;
            Skip();
        }

        var parts = new List<Part>();
        while (true)
        {
            Skip();
            Kind? kind = null;
            foreach (var (tok, k) in KindTokens)
            {
                if (!At(tok)) continue;
                kind = k;
                pos += tok.Length;
                break;
            }
            if (kind is null) break;

            if (kind == Kind.F)
            {
                // "F Fl" is fixed and flashing
                var save = pos;
                Skip();
                if (At("Fl") && !At("Fl+")) { kind = Kind.FFl; pos += 2; }
                else pos = save;
            }

            var groups = new List<int>();
            string? letters = null;
            Skip();
            if (pos < s.Length && s[pos] == '(')
            {
                var close = s.IndexOf(')', pos);
                if (close < 0) { error = "A bracket is not closed."; return false; }
                var inner = s[(pos + 1)..close].Replace(" ", "");
                pos = close + 1;
                if (kind == Kind.Mo)
                {
                    if (inner.Length == 0 || inner.Any(c => Morse.For(c) is null))
                    {
                        error = $"Mo({inner}) – put a Morse letter in the brackets, e.g. Mo(A).";
                        return false;
                    }
                    letters = inner.ToUpperInvariant();
                }
                else
                {
                    foreach (var g in inner.Split('+'))
                    {
                        if (!int.TryParse(g, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1 || n > 20)
                        {
                            error = $"({inner}) – the brackets should hold a number of flashes, e.g. (2) or (2+1).";
                            return false;
                        }
                        groups.Add(n);
                    }
                }
            }
            if (kind == Kind.Mo && letters is null)
            {
                error = "Morse lights need a letter, e.g. Mo(A).";
                return false;
            }
            if (groups.Count > 0 && kind is Kind.F or Kind.Iso)
            {
                error = $"{kind} lights don't have groups.";
                return false;
            }
            parts.Add(new Part(kind.Value, groups, letters));

            Skip();
            if (pos < s.Length && s[pos] == '+') { pos++; continue; }
            break;
        }

        // colours
        var colours = new List<string>();
        while (true)
        {
            Skip();
            var found = false;
            foreach (var (code, _, _) in ColourTable.OrderByDescending(c => c.Code.Length))
            {
                if (!At(code)) continue;
                colours.Add(code);
                pos += code.Length;
                found = true;
                break;
            }
            if (!found) break;
        }

        Skip();
        if (pos < s.Length)
        {
            var rest = s[pos..].Trim();
            error = parts.Count == 0 && colours.Count == 0
                ? $"“{rest}” – start with a characteristic such as F, Fl, LFl, Q, VQ, Oc, Iso or Mo(A)."
                : $"Didn't understand “{rest}”.";
            return false;
        }

        if (parts.Count == 0)
        {
            if (alternating && colours.Count > 1) parts.Add(new Part(Kind.F, [], null));
            else
            {
                error = "Start with a characteristic such as F, Fl, LFl, Q, VQ, Oc, Iso or Mo(A).";
                return false;
            }
        }
        if (parts.Count > 1 && parts.Any(p => p.Kind is Kind.Oc or Kind.F or Kind.FFl or Kind.Iso))
        {
            error = "Only flashing characteristics can be combined with “+”, e.g. Q(6)+LFl.";
            return false;
        }
        if (colours.Count == 0)
        {
            colours.Add("W");
            notes.Add("No colour given – white assumed.");
        }
        if (colours.Distinct().Count() != colours.Count)
        {
            error = "A colour is listed twice.";
            return false;
        }
        if (alternating && colours.Count < 2)
        {
            error = "An alternating light (Al) needs at least two colours, e.g. Al WR.";
            return false;
        }
        if (period is <= 0 or > 120)
        {
            error = "The period should be between 0 and 120 seconds.";
            return false;
        }

        var continuous = parts.Count == 1 && parts[0].Groups.Count == 0 && parts[0].Kind is Kind.Q or Kind.VQ or Kind.UQ;
        var periodGiven = period is not null;
        double usedPeriod;
        if (parts.Count == 1 && parts[0].Kind == Kind.F && !alternating)
        {
            usedPeriod = 1;
            if (periodGiven) notes.Add("A fixed light has no period – the number was ignored.");
            periodGiven = false;
        }
        else if (continuous)
        {
            usedPeriod = Unit(parts[0].Kind).On + Unit(parts[0].Kind).Off;
            if (periodGiven) notes.Add($"Continuous {parts[0].Kind} has no period on the chart – drawn at its normal rate.");
            periodGiven = false;
        }
        else if (parts[0].Kind == Kind.Iso)
        {
            usedPeriod = period ?? 4;
            if (!periodGiven) notes.Add("No period given – 4 s assumed.");
        }
        else
        {
            var active = ActiveLength(parts, period ?? 10) * (alternating ? colours.Count : 1);
            if (period is { } p)
            {
                usedPeriod = p;
                if (parts[0].Kind != Kind.F && active > p * 0.9) notes.Add($"The flashes barely fit in {N(p)} s – they are drawn shorter than usual.");
            }
            else
            {
                usedPeriod = alternating && parts[0].Kind == Kind.F ? 2 * colours.Count : NicePeriod(active);
                notes.Add($"No period given – {N(usedPeriod)} s assumed.");
            }
        }

        var canonical = Canonical(parts, alternating, colours, periodGiven ? usedPeriod : null);
        var (name, desc) = Describe(parts, alternating, colours, usedPeriod, continuous);
        if (height is { } h) desc += $" The light is {N(h)} m above the height datum.";
        if (range is { } r) desc += $" Nominal range {N(r)} nautical miles.";
        if (!alternating && colours.Count > 1)
            desc += $" Several colours without “Al” means a sector light: you see {string.Join(", ", colours.Select(ColourName))} depending on your bearing from it.";

        result = new Result(input.Trim(), canonical, name, desc, parts, alternating, colours, usedPeriod, periodGiven, height, range, notes);
        return true;
    }

    private static double NicePeriod(double active)
    {
        var target = active * 1.6 + 2;
        double[] nice = [2, 2.5, 3, 4, 5, 6, 8, 10, 12, 15, 20, 30, 45, 60];
        return nice.FirstOrDefault(n => n >= target, Math.Ceiling(target));
    }

    // ------------------------------------------------------------------ timing

    /// <summary>Typical on/off times of one flash (or eclipse) and the gap used between groups.</summary>
    private static (double On, double Off, double GroupGap) Unit(Kind k) => k switch
    {
        Kind.Fl => (0.5, 0.8, 1.8),
        Kind.LFl => (2.0, 1.5, 2.5),
        Kind.Q or Kind.IQ => (0.3, 0.7, 1.5),
        Kind.VQ or Kind.IVQ => (0.15, 0.35, 0.8),
        Kind.UQ or Kind.IUQ => (0.08, 0.17, 0.5),
        Kind.Oc => (1.0, 1.0, 2.0),     // eclipse, light between eclipses, light between groups
        Kind.FFl => (0.8, 1.0, 1.8),
        _ => (1, 1, 1),
    };

    /// <summary>A pulse is an "event" (flash, or eclipse for Oc); a gap separates events.</summary>
    private static List<(double Dur, bool Event)> Pulses(Part p, double period)
    {
        var list = new List<(double, bool)>();
        var (on, off, groupGap) = Unit(p.Kind);
        switch (p.Kind)
        {
            case Kind.Mo:
                var firstLetter = true;
                foreach (var ch in p.Letters!)
                {
                    if (!firstLetter) list.Add((1.5, false));
                    firstLetter = false;
                    var code = Morse.For(ch)!;
                    for (var i = 0; i < code.Length; i++)
                    {
                        if (i > 0) list.Add((0.5, false));
                        list.Add((code[i] == '.' ? 0.5 : 1.5, true));
                    }
                }
                return list;
            case Kind.Iso:
                list.Add((period / 2, true));
                return list;
            case Kind.F:
                list.Add((period, true));
                return list;
            case Kind.IQ or Kind.IVQ or Kind.IUQ:
            {
                // interrupted: a run of quick flashes, then a long eclipse (about a third of the period)
                var n = Math.Max(2, (int)Math.Floor(period * 0.65 / (on + off)));
                for (var i = 0; i < n; i++)
                {
                    if (i > 0) list.Add((off, false));
                    list.Add((on, true));
                }
                return list;
            }
        }

        if (p.Kind == Kind.Fl && p.Groups.Count == 0) on = period >= 4 ? 1.0 : Math.Max(0.3, period * 0.2);
        var groups = p.Groups.Count == 0 ? [1] : p.Groups;
        for (var g = 0; g < groups.Count; g++)
        {
            if (g > 0) list.Add((groupGap, false));
            for (var i = 0; i < groups[g]; i++)
            {
                if (i > 0) list.Add((off, false));
                list.Add((on, true));
            }
        }
        return list;
    }

    private static double ActiveLength(IReadOnlyList<Part> parts, double period = 10)
    {
        double sum = 0;
        for (var i = 0; i < parts.Count; i++)
        {
            if (i > 0) sum += Unit(parts[i - 1].Kind).Off;
            sum += Pulses(parts[i], period).Sum(x => x.Dur);
        }
        return sum;
    }

    private static List<Seg> BuildCycle(IReadOnlyList<Part> parts, IReadOnlyList<string> hexes, double period)
    {
        var k = hexes.Count;
        var sub = period / k;      // alternating lights share the period between the colours
        var cycle = new List<Seg>();
        var first = parts[0].Kind;
        var continuous = parts.Count == 1 && parts[0].Groups.Count == 0 && first is Kind.Q or Kind.VQ or Kind.UQ;

        foreach (var hex in hexes)
        {
            if (first == Kind.F)
            {
                cycle.Add(new Seg(sub, hex));
                continue;
            }
            if (continuous)
            {
                var (on, off, _) = Unit(first);
                cycle.Add(new Seg(on, hex));
                cycle.Add(new Seg(off, null));
                continue;
            }

            var pulses = new List<(double Dur, bool Event)>();
            for (var i = 0; i < parts.Count; i++)
            {
                if (i > 0) pulses.Add((Unit(parts[i - 1].Kind).Off, false));
                pulses.AddRange(Pulses(parts[i], sub));
            }
            var active = pulses.Sum(x => x.Dur);
            var limit = first == Kind.Iso ? sub : sub * 0.9;
            if (active > limit)
            {
                var f = limit / active;
                pulses = pulses.Select(x => (x.Dur * f, x.Event)).ToList();
                active = limit;
            }
            pulses.Add((Math.Max(0, sub - active), false));

            foreach (var (dur, ev) in pulses)
            {
                if (dur <= 0.0005) continue;
                var seg = first switch
                {
                    Kind.Oc => ev ? new Seg(dur, null) : new Seg(dur, hex),          // eclipses in a steady light
                    Kind.FFl => ev ? new Seg(dur, hex, 1) : new Seg(dur, hex, 0.45), // brighter flashes on a steady light
                    _ => ev ? new Seg(dur, hex) : new Seg(dur, null),
                };
                cycle.Add(seg);
            }
        }

        // merge neighbours with the same state (keeps the band and the animation tidy)
        var merged = new List<Seg>();
        foreach (var s in cycle)
        {
            if (merged.Count > 0 && merged[^1].Colour == s.Colour && Math.Abs(merged[^1].Level - s.Level) < 1e-9)
                merged[^1] = merged[^1] with { Seconds = merged[^1].Seconds + s.Seconds };
            else merged.Add(s);
        }
        return merged;
    }

    // ------------------------------------------------------------------ words

    private static string Token(Kind k) => k switch { Kind.FFl => "F Fl", _ => k.ToString() };

    private static string Canonical(IReadOnlyList<Part> parts, bool alt, IReadOnlyList<string> colours, double? period)
    {
        var sb = new StringBuilder();
        if (alt) sb.Append("Al.");
        var onlyF = parts.Count == 1 && parts[0].Kind == Kind.F;
        if (!(alt && onlyF))
        {
            sb.Append(string.Join("+", parts.Select(p =>
                Token(p.Kind) + (p.Letters is not null ? $"({p.Letters})" : p.Groups.Count > 0 ? $"({string.Join("+", p.Groups)})" : ""))));
            sb.Append(' ');
        }
        sb.Append(string.Join("", colours));
        if (period is { } per) sb.Append(' ').Append(N(per)).Append('s');
        return sb.ToString();
    }

    private static string KindName(Kind k) => k switch
    {
        Kind.F => "fixed",
        Kind.Fl => "flashing",
        Kind.LFl => "long flashing",
        Kind.Q => "quick flashing",
        Kind.VQ => "very quick flashing",
        Kind.UQ => "ultra quick flashing",
        Kind.IQ => "interrupted quick flashing",
        Kind.IVQ => "interrupted very quick flashing",
        Kind.IUQ => "interrupted ultra quick flashing",
        Kind.Oc => "occulting",
        Kind.Iso => "isophase",
        Kind.Mo => "Morse code",
        Kind.FFl => "fixed and flashing",
        _ => k.ToString(),
    };

    private static string EventWord(Kind k, int n) => (k, n == 1) switch
    {
        (Kind.Oc, true) => "eclipse",
        (Kind.Oc, false) => "eclipses",
        (Kind.LFl, true) => "long flash (2 s or more)",
        (Kind.LFl, false) => "long flashes",
        (Kind.Q or Kind.VQ or Kind.UQ, true) => KindName(k).Replace(" flashing", "") + " flash",
        (Kind.Q or Kind.VQ or Kind.UQ, false) => KindName(k).Replace(" flashing", "") + " flashes",
        (Kind.FFl, true) => "brighter flash",
        (Kind.FFl, false) => "brighter flashes",
        (_, true) => "flash",
        _ => "flashes",
    };

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string Count(int n) => n switch
    {
        1 => "one", 2 => "two", 3 => "three", 4 => "four", 5 => "five", 6 => "six", 7 => "seven", 8 => "eight", 9 => "nine",
        _ => n.ToString(CultureInfo.InvariantCulture),
    };

    private static (string Name, string Desc) Describe(IReadOnlyList<Part> parts, bool alt, IReadOnlyList<string> colours, double period, bool continuous)
    {
        string PartName(Part p)
        {
            var baseName = KindName(p.Kind);
            if (p.Kind == Kind.Mo) return $"Morse code ({p.Letters})";
            return p.Groups.Count switch
            {
                0 => baseName,
                1 => "group " + baseName,
                _ => "composite group " + baseName,
            };
        }

        string PartText(Part p)
        {
            switch (p.Kind)
            {
                case Kind.F: return "a steady light";
                case Kind.Iso: return "light and dark of exactly equal length";
                case Kind.Mo:
                    return "the Morse letter" + (p.Letters!.Length > 1 ? "s " : " ") + p.Letters + " (" +
                           string.Join(" ", p.Letters.Select(c => Morse.For(c)!.Replace('.', '·').Replace('-', '–'))) + ")";
                case Kind.IQ or Kind.IVQ or Kind.IUQ:
                    return $"a run of {KindName(p.Kind).Replace("interrupted ", "").Replace(" flashing", "")} flashes broken by a long eclipse";
            }
            if (p.Groups.Count == 0)
                return p.Kind switch
                {
                    Kind.Q or Kind.VQ or Kind.UQ when continuous => $"continuous {KindName(p.Kind)} ({(p.Kind == Kind.Q ? "50–60" : p.Kind == Kind.VQ ? "100–120" : "160 or more")} a minute)",
                    Kind.Oc => "a single eclipse",
                    _ => "a single " + EventWord(p.Kind, 1),
                };
            if (p.Groups.Count == 1)
                return $"a group of {Count(p.Groups[0])} {EventWord(p.Kind, p.Groups[0])}";
            return "groups of " + string.Join(" then ", p.Groups.Select(Count)) + " " + EventWord(p.Kind, 2);
        }

        var name = string.Join(" + ", parts.Select(PartName));
        if (alt) name = parts.Count == 1 && parts[0].Kind == Kind.F ? "alternating" : "alternating " + name;
        name = Cap(name);

        var colourWords = alt
            ? "alternating " + string.Join(" and ", colours.Select(ColourName))
            : string.Join(" / ", colours.Select(ColourName));

        var body = string.Join(", followed by ", parts.Select(PartText));
        var first = parts[0].Kind;
        var background = first switch
        {
            Kind.F or Kind.Iso => "",
            Kind.Oc => " The light is on the rest of the time.",
            Kind.FFl => " Between the flashes a weaker steady light shows.",
            _ when continuous => "",
            _ => " Then dark until the cycle starts again.",
        };
        var periodText = continuous || (first == Kind.F && !alt) ? "" : $" One full cycle (the period) takes {N(period)} s.";
        if (alt && first == Kind.F) body = "the colour changes in a regular sequence, with no dark interval";
        return (name, $"{Cap(body)}, {colourWords}.{background}{periodText}");
    }
}
