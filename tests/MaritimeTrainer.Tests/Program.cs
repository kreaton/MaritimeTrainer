using MaritimeTrainer.Models;

// Checks the model logic without a browser. The exit code is the number of failed checks.
var failures = 0;
var checks = 0;
void Check(bool cond, string what)
{
    checks++;
    if (!cond) { failures++; Console.WriteLine("FAIL: " + what); }
}
static bool Near(double a, double b) => Math.Abs(a - b) < 1e-6;

// ---------- Morse ----------
Check(Morse.For('A') == ".-", "Morse A");
Check(Morse.Encode("SOS") == "... --- ...", "Morse SOS");
Check(Morse.Encode("HI YOU") == ".... ../-.-- --- ..-", "Morse word gap");
Check(Morse.Normalize("·–·") == ".-.", "Morse normalize");

// ---------- Light parser: things that should be read ----------
LightParser.Result P(string s)
{
    var ok = LightParser.TryParse(s, out var r, out var err);
    Check(ok, $"parses \"{s}\" ({err})");
    return r!;
}

var fl3 = P("Fl(3) G 10s");
Check(fl3.Canonical == "Fl(3) G 10s", "canonical Fl(3) G 10s");
Check(Near(fl3.ToSpec().Period, 10), "Fl(3) period is 10 s");
Check(fl3.ToSpec().Cycle.Count(c => c.Colour is not null) == 3, "Fl(3) has three flashes");
Check(fl3.ToSpec().Cycle.All(c => c.Colour is null || c.Colour == Lights.G), "Fl(3) G is green");

var south = P("Q(6)+LFl W 15s");
var sc = south.ToSpec().Cycle;
Check(sc.Count(c => c.Colour is not null) == 7, "Q(6)+LFl: six quick flashes and one long");
Check(sc.Where(c => c.Colour is not null).Max(c => c.Seconds) >= 2, "Q(6)+LFl: the long flash is at least 2 s");
Check(Near(south.ToSpec().Period, 15), "Q(6)+LFl period 15 s");

var oc = P("Oc(2+1) R 12s");
Check(oc.ToSpec().Cycle.Count(c => c.Colour is null) == 3, "Oc(2+1): three eclipses");
Check(oc.ToSpec().Cycle.Where(c => c.Colour is not null).Sum(c => c.Seconds) > 6, "Oc: lit longer than dark");

var iso = P("Iso G 4s").ToSpec().Cycle;
Check(Near(iso.Where(c => c.Colour is not null).Sum(c => c.Seconds), 2), "Iso: light = dark");

var mo = P("Mo(U) W 15s").ToSpec().Cycle;
Check(mo.Count(c => c.Colour is not null) == 3, "Mo(U): three elements");

var q = P("Q");
Check(q.Notes.Any(n => n.Contains("white")), "Q with no colour assumes white");
Check(Near(q.ToSpec().Period, 1), "continuous Q has a 1 s rhythm");

var chart = P("Fl.R.5s");
Check(chart.Canonical == "Fl R 5s", "chart style with dots");

var lower = P("fl(2) w 2,5s");
Check(lower.Canonical == "Fl(2) W 2.5s", "lower case and decimal comma");

var full = P("Oc G 4s 12m 7M");
Check(full.HeightM == 12 && full.RangeNm == 7, "height and range");

var alt = P("Al WR 10s");
Check(alt.Alternating && alt.ToSpec().Cycle.All(c => c.Colour is not null), "Al WR: no dark interval");
Check(alt.Notes.Count == 0, "Al WR: no spurious notes");

var sector = P("Iso WRG 4s");
Check(sector.IsSector, "WRG without Al is a sector light");
Check(sector.ToSpec("R").Cycle.Any(c => c.Colour == Lights.R), "sector light shown in the red sector");
Check(sector.InColour("#123456").Cycle.Any(c => c.Colour == "#123456"), "InColour draws the rhythm in any colour");

var ffl = P("F Fl(2) W 10s").ToSpec().Cycle;
Check(ffl.All(c => c.Colour is not null) && ffl.Any(c => c.Level < 1), "F Fl: never dark, dim between flashes");

var noPeriod = P("Fl(2+1) G");
Check(noPeriod.Notes.Any(n => n.Contains("No period")), "missing period is assumed and noted");

// every built-in light's abbreviation should be readable, with the same period
foreach (var spec in Lights.All.Where(s => !s.Abbr.StartsWith("Al")))
{
    var ok = LightParser.TryParse(spec.Abbr, out var r, out var err);
    Check(ok, $"built-in light {spec.Abbr} parses ({err})");
    if (ok && Lights.ParsePeriod(spec.Abbr) is { } per)
        Check(Near(r!.Period, per), $"built-in light {spec.Abbr} keeps its period");
}

// ---------- Light parser: things that should be refused ----------
foreach (var bad in new[] { "", "Xyz", "Fl(2 W 5s", "Mo W 6s", "Fl(x) W 5s", "Al W 5s", "Fl WW 5s", "Oc+Fl W 5s" })
    Check(!LightParser.TryParse(bad, out _, out var e) && e.Length > 0, $"refuses \"{bad}\"");

// ---------- Sector lights ----------
Check(SectorData.TryParseRows("R 245°-260°, W -265°, G -280°", out var rows, out _), "light-list style sectors");
Check(rows.Count == 3 && rows[1].From == 260 && rows[2].To == 280, "missing start continues from the last sector");

var errors = new List<string>();
var warnings = new List<string>();
var seaward = SectorData.BuildLight(rows, fromSeaward: true, "Oc WRG 6s", errors, warnings)!;
Check(errors.Count == 0 && warnings.Count == 0, "valid sectors give no errors");
// from seaward 262° (white) = 082° from the light
Check(seaward.ColourAt(82) == "W", "seaward bearings are turned round 180°");
Check(seaward.ColourAt(70) == "R" && seaward.ColourAt(90) == "G", "red and green either side of white");
Check(seaward.ColourAt(262) == "-", "the light is not visible on the far side");

var wrap = new Sector(350, 10, "W");
Check(wrap.Contains(355) && wrap.Contains(5) && !wrap.Contains(10), "a sector can cross north");

SectorData.TryParseRows("W 10-20, R 15-30", out var overlap, out _);
errors.Clear(); warnings.Clear();
SectorData.BuildLight(overlap, false, "", errors, warnings);
Check(warnings.Count == 1, "overlapping sectors are reported");

Check(!SectorData.TryParseRows("X 1-2", out _, out _), "unknown sector colour is refused");

Console.WriteLine(failures == 0 ? $"All {checks} checks passed." : $"{failures} of {checks} checks failed.");
return failures;
