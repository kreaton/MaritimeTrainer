using System.Globalization;
using System.Text.RegularExpressions;

namespace MaritimeTrainer.Models;

/// <summary>An arc of a sector light. Bearings are degrees true <b>from the light</b> to the observer, clockwise.</summary>
public sealed record Sector(int From, int To, string Colour)
{
    public bool Contains(int bearing)
    {
        bearing = ((bearing % 360) + 360) % 360;
        return From <= To ? bearing >= From && bearing < To : bearing >= From || bearing < To;
    }

    public int Width => ((To - From) % 360 + 360) % 360;
}

public sealed record SectorLight(string Name, IReadOnlyList<Sector> Sectors, string Abbr)
{
    /// <summary>"W", "R", "G" or "-" (not visible) for an observer at the given bearing from the light.</summary>
    public string ColourAt(int bearing)
    {
        foreach (var s in Sectors)
            if (s.Contains(bearing)) return s.Colour;
        return "-";
    }
}

public static partial class SectorData
{
    public static readonly string[] SectorColours = ["W", "R", "G", "Y"];

    /// <summary>One sector as the user typed it (in their chosen bearing convention).</summary>
    public sealed class Row
    {
        public string Colour { get; set; } = "W";
        public int From { get; set; }
        public int To { get; set; }
    }

    public static int Norm(int b) => ((b % 360) + 360) % 360;

    /// <summary>"R 245-260, W 260-265, G 265-280"</summary>
    public static string Format(IEnumerable<Row> rows) =>
        string.Join(", ", rows.Select(r => $"{r.Colour} {r.From:000}-{r.To:000}"));

    [GeneratedRegex(@"^(W|R|G|Y|white|red|green|yellow)(?![a-z])\s*:?\s*(?:(\d{1,3}(?:[.,]\d+)?)\s*°?)?\s*(?:-|–|—|to)\s*(\d{1,3}(?:[.,]\d+)?)\s*°?$", RegexOptions.IgnoreCase)]
    private static partial Regex RowRx();

    /// <summary>
    /// Reads sectors written like a light list: "R 245°-260°, W -265°, G -280°" (a missing start
    /// continues from the previous sector). Entries are separated by commas, semicolons or new lines.
    /// </summary>
    public static bool TryParseRows(string text, out List<Row> rows, out string error)
    {
        rows = [];
        error = "";
        var entries = Regex.Split(text ?? "", @"[,;\n\r]+").Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
        if (entries.Count == 0) { error = "Enter at least one sector."; return false; }
        int? prevTo = null;
        foreach (var e in entries)
        {
            var m = RowRx().Match(e);
            if (!m.Success)
            {
                error = $"“{e}” – write a sector as colour, start and end, e.g. W 260-265.";
                return false;
            }
            var colour = char.ToUpperInvariant(m.Groups[1].Value[0]).ToString();
            int? from = m.Groups[2].Success ? Deg(m.Groups[2].Value) : prevTo;
            if (from is null)
            {
                error = $"“{e}” – the first sector needs a start bearing.";
                return false;
            }
            var to = Deg(m.Groups[3].Value);
            rows.Add(new Row { Colour = colour, From = from.Value, To = to });
            prevTo = to;
        }
        return true;
    }

    private static int Deg(string s) =>
        Norm((int)Math.Round(double.Parse(s.Replace(',', '.'), CultureInfo.InvariantCulture), MidpointRounding.AwayFromZero));

    /// <summary>
    /// Turns the user's rows into a light. Bearings from seaward (observer → light, as printed in light lists)
    /// are turned round by 180° because the diagram and the drill work from the light outwards.
    /// </summary>
    public static SectorLight? BuildLight(IReadOnlyList<Row> rows, bool fromSeaward, string abbr, List<string> errors, List<string> warnings)
    {
        if (rows.Count == 0) { errors.Add("Add at least one sector."); return null; }
        var sectors = new List<Sector>();
        foreach (var r in rows)
        {
            if (r.From is < 0 or > 360 || r.To is < 0 or > 360)
            {
                errors.Add($"{ColourName(r.Colour)} sector: bearings must be between 0 and 360.");
                continue;
            }
            if (Norm(r.From) == Norm(r.To) && rows.Count > 1)
            {
                errors.Add($"{ColourName(r.Colour)} sector {r.From:000}–{r.To:000}: start and end are the same.");
                continue;
            }
            var off = fromSeaward ? 180 : 0;
            sectors.Add(new Sector(Norm(r.From + off), Norm(r.To + off), r.Colour));
        }
        if (errors.Count > 0) return null;

        var light = new SectorLight("Your sector light", sectors, string.IsNullOrWhiteSpace(abbr) ? "Sector light" : abbr.Trim());
        var overlapping = Enumerable.Range(0, 360).Where(b => sectors.Count(x => x.Contains(b)) > 1).ToList();
        if (overlapping.Count > 0)
        {
            var shown = fromSeaward ? Norm(overlapping[0] + 180) : overlapping[0];
            warnings.Add($"Sectors overlap ({overlapping.Count}° in all, e.g. at {shown:000}°) – the first one listed wins there.");
        }
        return light;
    }

    public static string ColourName(string code) => code switch
    {
        "W" => "White",
        "R" => "Red",
        "G" => "Green",
        "Y" => "Yellow",
        _ => "Not visible",
    };

    public static string ColourHex(string code) => code switch
    {
        "W" => "#fff2b0",
        "R" => "#ff4a3d",
        "G" => "#35d16f",
        "Y" => "#ffd60a",
        _ => "#1b2a3d",
    };

    /// <summary>Makes a random light: a red, white and green sector side by side, plus an unlit arc.</summary>
    public static SectorLight Random(Random rng)
    {
        var start = rng.Next(0, 72) * 5;
        var rW = rng.Next(3, 9) * 5;   // red 15–40°
        var wW = rng.Next(3, 10) * 5;  // white 15–45°
        var gW = rng.Next(3, 9) * 5;   // green 15–40°
        var order = rng.Next(2) == 0 ? new[] { ("R", rW), ("W", wW), ("G", gW) } : new[] { ("G", gW), ("W", wW), ("R", rW) };

        var sectors = new List<Sector>();
        var cur = start;
        foreach (var (c, w) in order)
        {
            var to = (cur + w) % 360;
            sectors.Add(new Sector(cur, to, c));
            cur = to;
        }
        var period = new[] { 5, 8, 10, 12, 15 }[rng.Next(5)];
        var rhythm = new[] { "Fl", "Oc", "Iso" }[rng.Next(3)];
        return new SectorLight("Sector light", sectors, $"{rhythm} WRG {period}s");
    }
}
