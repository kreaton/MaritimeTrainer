namespace MaritimeTrainer.Models;

public enum BodyShape { Can, Cone, Sphere, Pillar }

public enum TopmarkKind
{
    None, Can, Cone, Sphere, TwoSpheres,
    ConesUp, ConesDown, ConesBaseToBase, ConesPointToPoint,
    SaintAndrewsCross, UprightCross,
}

/// <summary>A horizontal colour band (top to bottom, sized by weight).</summary>
public readonly record struct Band(string Colour, double Weight = 1);

public sealed record MarkSpec(
    string Id,
    string Name,
    string Group,               // lateral | cardinal | other
    BodyShape Shape,
    Band[] Bands,               // horizontal bands, top to bottom
    string[]? Stripes,          // vertical stripes left to right (overrides Bands)
    TopmarkKind Top,
    string TopColour,
    string Colours,             // colours in words
    string Passing,
    string Notes,
    string? LightKey,           // example light (for animation)
    string? LightAbbr,          // chart abbreviation(s) to learn
    bool LightIsDistinctive,    // true when the rhythm identifies the mark (used in quizzes)
    string LightNote)           // rhythm rules in words
{
    public string TopmarkName => Top switch
    {
        TopmarkKind.None => "None",
        TopmarkKind.Can => "Single cylinder (can)",
        TopmarkKind.Cone => "Single cone, point up",
        TopmarkKind.Sphere => "Single sphere",
        TopmarkKind.TwoSpheres => "Two spheres, one above the other",
        TopmarkKind.ConesUp => "Two cones, points up",
        TopmarkKind.ConesDown => "Two cones, points down",
        TopmarkKind.ConesBaseToBase => "Two cones, base to base",
        TopmarkKind.ConesPointToPoint => "Two cones, point to point",
        TopmarkKind.SaintAndrewsCross => "Single X-shaped cross",
        TopmarkKind.UprightCross => "Single upright cross",
        _ => "",
    };
}

/// <summary>The IALA Maritime Buoyage System (R1001): lateral, cardinal, isolated danger, safe water, special and emergency wreck marks.</summary>
public static class Marks
{
    private const string R = Palette.Red, G = Palette.Green, K = Palette.Black, Y = Palette.Yellow, W = Palette.White, Bu = Palette.Blue;

    private static Band[] Bands(params string[] c) => c.Select(x => new Band(x)).ToArray();

    public static List<MarkSpec> Build(Region region)
    {
        var a = region == Region.A;
        var portCol = a ? R : G;
        var stbdCol = a ? G : R;
        var portName = a ? "red" : "green";
        var stbdName = a ? "green" : "red";
        var portLight = a ? "FlR" : "FlG";
        var stbdLight = a ? "FlG" : "FlR";
        var prefStbdLight = a ? "Fl21" : "Fl21G";
        var prefPortLight = a ? "Fl21G" : "Fl21";

        return
        [
            new("port", "Port-hand lateral mark", "lateral", BodyShape.Can, Bands(portCol), null, TopmarkKind.Can, portCol,
                $"All {portName}", "Keep it on your port (left) side when heading in the direction of buoyage.",
                $"Marks the left-hand side of a channel (can shape, {portName}).",
                portLight, $"Fl {(a ? "R" : "G")} (any rhythm)", false,
                $"{(a ? "Red" : "Green")} light, any rhythm except Fl(2+1)."),

            new("starboard", "Starboard-hand lateral mark", "lateral", BodyShape.Cone, Bands(stbdCol), null, TopmarkKind.Cone, stbdCol,
                $"All {stbdName}", "Keep it on your starboard (right) side when heading in the direction of buoyage.",
                $"Marks the right-hand side of a channel (conical shape, {stbdName}).",
                stbdLight, $"Fl {(a ? "G" : "R")} (any rhythm)", false,
                $"{(a ? "Green" : "Red")} light, any rhythm except Fl(2+1)."),

            new("prefstbd", "Preferred channel to starboard", "lateral", BodyShape.Pillar,
                a ? Bands(R, G, R) : Bands(G, R, G), null, TopmarkKind.Can, portCol,
                a ? "Red, with one broad green band" : "Green, with one broad red band",
                "The channel divides: the preferred (main) channel is to starboard. Keep the mark on your port side to follow it.",
                "A modified port-hand mark placed where a channel divides.",
                prefStbdLight, a ? "Fl(2+1) R" : "Fl(2+1) G", true,
                $"Composite group flashing (2+1), {(a ? "red" : "green")}."),

            new("prefport", "Preferred channel to port", "lateral", BodyShape.Pillar,
                a ? Bands(G, R, G) : Bands(R, G, R), null, TopmarkKind.Cone, stbdCol,
                a ? "Green, with one broad red band" : "Red, with one broad green band",
                "The channel divides: the preferred (main) channel is to port. Keep the mark on your starboard side to follow it.",
                "A modified starboard-hand mark placed where a channel divides.",
                prefPortLight, a ? "Fl(2+1) G" : "Fl(2+1) R", true,
                $"Composite group flashing (2+1), {(a ? "green" : "red")}."),

            new("north", "North cardinal", "cardinal", BodyShape.Pillar, new[] { new Band(K), new Band(Y) }, null,
                TopmarkKind.ConesUp, K, "Black above yellow",
                "Safe water lies to the north of the mark – pass north of it.",
                "Both cones point up – the black is on top, towards the north.",
                "Q", "Q W  or  VQ W (continuous)", true,
                "White, continuous quick (Q) or very quick (VQ) flashing."),

            new("east", "East cardinal", "cardinal", BodyShape.Pillar, new[] { new Band(K), new Band(Y), new Band(K) }, null,
                TopmarkKind.ConesBaseToBase, K, "Black with a single broad yellow band",
                "Safe water lies to the east of the mark – pass east of it.",
                "Cones base to base (egg shape) – “E” for egg.",
                "Q3", "Q(3) W 10s  or  VQ(3) W 5s", true,
                "White, three quick flashes (like 3 o'clock on a clock face)."),

            new("south", "South cardinal", "cardinal", BodyShape.Pillar, new[] { new Band(Y), new Band(K) }, null,
                TopmarkKind.ConesDown, K, "Yellow above black",
                "Safe water lies to the south of the mark – pass south of it.",
                "Both cones point down – the black is at the bottom, towards the south.",
                "Q6LFl", "Q(6)+LFl W 15s  or  VQ(6)+LFl W 10s", true,
                "White, six quick flashes plus one long flash (6 o'clock)."),

            new("west", "West cardinal", "cardinal", BodyShape.Pillar, new[] { new Band(Y), new Band(K), new Band(Y) }, null,
                TopmarkKind.ConesPointToPoint, K, "Yellow with a single broad black band",
                "Safe water lies to the west of the mark – pass west of it.",
                "Cones point to point (wine-glass shape) – “W” for wine glass.",
                "Q9", "Q(9) W 15s  or  VQ(9) W 10s", true,
                "White, nine quick flashes (9 o'clock)."),

            new("isolated", "Isolated danger", "other", BodyShape.Pillar, new[] { new Band(K), new Band(R), new Band(K) }, null,
                TopmarkKind.TwoSpheres, K, "Black with one or more broad red bands",
                "A danger with navigable water all around it – keep clear and pass at a safe distance on any side.",
                "Moored on or above a small danger. Two black spheres.",
                "Fl2b", "Fl(2) W 5s", true,
                "White, group flashing of two."),

            new("safe", "Safe water (mid-channel)", "other", BodyShape.Sphere, [], new[] { R, W, R, W, R, W }, TopmarkKind.Sphere, R,
                "Red and white vertical stripes",
                "Navigable water all around – a mid-channel or landfall mark; you may pass on any side.",
                "Marks the middle of a channel, a fairway or a landfall. Spherical, or a pillar/spar with a single red ball topmark.",
                "LFl", "Iso W · Oc W · LFl W 10s · Mo(A) W", true,
                "White: isophase, occulting, one long flash every 10 s, or Morse “A”."),

            new("special", "Special mark", "other", BodyShape.Pillar, Bands(Y), null, TopmarkKind.SaintAndrewsCross, Y,
                "Yellow",
                "Not primarily for navigation – marks a special area or feature (outfall, cable, exercise area, etc.). Check the chart.",
                "Any shape that does not conflict with navigational marks. Yellow X topmark.",
                "FlY", "Fl Y (any rhythm)", false,
                "Yellow, any rhythm that is not used for white lights on cardinal, isolated-danger or safe-water marks."),

            new("wreck", "Emergency wreck marking", "other", BodyShape.Pillar, [], new[] { Bu, Y, Bu, Y, Bu, Y }, TopmarkKind.UprightCross, Y,
                "Blue and yellow vertical stripes",
                "Marks a new, uncharted wreck or danger – keep clear and pass at a safe distance.",
                "Used for a limited time until the danger is charted or marked conventionally. Yellow upright cross.",
                "AlBuY", "Al Bu Y (alternating blue and yellow)", true,
                "Blue and yellow lights flashing alternately."),
        ];
    }

    public static MarkSpec Get(string id, Region region) => Build(region).First(m => m.Id == id);

    public static List<QuizItem> BuildItems(string pool, Region region)
    {
        var specs = Build(region).Where(m => pool is "all" or "" || m.Group == pool);
        var items = new List<QuizItem>();
        foreach (var m in specs)
        {
            var item = new QuizItem
            {
                // Lateral marks are learned separately per region because their colours differ.
                Id = m.Group == "lateral" ? $"mark:{m.Id}:{region}" : "mark:" + m.Id,
                Deck = "buoys",
                Label = m.Name,
                Group = m.Group,
            };
            item.With(Rep.MarkImage, m.Id)
                .With(Rep.MarkName, m.Name)
                .With(Rep.MarkPassing, m.Passing)
                .With(Rep.MarkColours, m.Colours);
            // A port-hand mark and a "preferred channel to starboard" mark carry the same topmark (likewise for
            // starboard), so lateral topmarks are not used in quizzes – they would have two right answers.
            if (m.Top != TopmarkKind.None && m.Group != "lateral") item.With(Rep.Topmark, m.Id);
            if (m.LightIsDistinctive && m.LightKey is not null)
            {
                item.With(Rep.LightAnim, m.LightKey);
                item.With(Rep.LightAbbr, m.LightAbbr!);
            }
            items.Add(item);
        }
        return items;
    }
}
