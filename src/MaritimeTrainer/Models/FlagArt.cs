using System.Globalization;
using System.Text;

namespace MaritimeTrainer.Models;

public static class Palette
{
    public const string Red = "#d7261e";
    public const string Green = "#12994a";
    public const string Blue = "#0b3d91";
    public const string Yellow = "#ffd400";
    public const string White = "#ffffff";
    public const string Black = "#151515";
}

/// <summary>Draws the International Code of Signals flags and pennants as inline SVG.</summary>
public static class FlagArt
{
    private const string R = Palette.Red, B = Palette.Blue, Y = Palette.Yellow, W = Palette.White, K = Palette.Black;

    public sealed record Art(int W, int H, string Outline, string Body);

    // Geometry
    private const string RectOutline = "0,0 120,0 120,100 0,100";
    private const string SwallowOutline = "0,0 150,0 105,50 150,100 0,100";
    private const string PennantOutline = "0,0 180,38 180,62 0,100";

    public static bool Exists(string id) => id.Length == 1 && char.IsLetterOrDigit(id[0]) || id == "ANS";

    private static string Rect(double x, double y, double w, double h, string fill) =>
        $"<rect x=\"{N(x)}\" y=\"{N(y)}\" width=\"{N(w)}\" height=\"{N(h)}\" fill=\"{fill}\"/>";

    private static string Poly(string points, string fill) => $"<polygon points=\"{points}\" fill=\"{fill}\"/>";

    private static string Circle(double cx, double cy, double r, string fill) =>
        $"<circle cx=\"{N(cx)}\" cy=\"{N(cy)}\" r=\"{N(r)}\" fill=\"{fill}\"/>";

    private static string Line(double x1, double y1, double x2, double y2, string stroke, double w) =>
        $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" stroke=\"{stroke}\" stroke-width=\"{N(w)}\"/>";

    private static string N(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    private static string VStripes(int w, int h, params string[] cols)
    {
        var sb = new StringBuilder();
        var sw = (double)w / cols.Length;
        for (var i = 0; i < cols.Length; i++) sb.Append(Rect(i * sw, 0, sw + 0.5, h, cols[i]));
        return sb.ToString();
    }

    private static string HStripes(int w, int h, params string[] cols)
    {
        var sb = new StringBuilder();
        var sh = (double)h / cols.Length;
        for (var i = 0; i < cols.Length; i++) sb.Append(Rect(0, i * sh, w, sh + 0.5, cols[i]));
        return sb.ToString();
    }

    public static Art Get(string id)
    {
        id = id.ToUpperInvariant();
        string body;
        var outline = RectOutline;
        int w = 120, h = 100;

        switch (id)
        {
            // ----- Letters -----
            case "A": outline = SwallowOutline; w = 150; body = Rect(0, 0, 75, 100, W) + Rect(75, 0, 75, 100, B); break;
            case "B": outline = SwallowOutline; w = 150; body = Rect(0, 0, 150, 100, R); break;
            case "C": body = HStripes(120, 100, B, W, R, W, B); break;
            case "D": body = Rect(0, 0, 120, 100, Y) + Rect(0, 25, 120, 50, B); break;
            case "E": body = HStripes(120, 100, B, R); break;
            case "F": body = Rect(0, 0, 120, 100, W) + Poly("60,0 120,50 60,100 0,50", R); break;
            case "G": body = VStripes(120, 100, Y, B, Y, B, Y, B); break;
            case "H": body = VStripes(120, 100, W, R); break;
            case "I": body = Rect(0, 0, 120, 100, Y) + Circle(60, 50, 24, K); break;
            case "J": body = HStripes(120, 100, B, W, B); break;
            case "K": body = VStripes(120, 100, Y, B); break;
            case "L": body = Rect(0, 0, 60, 50, Y) + Rect(60, 0, 60, 50, K) + Rect(0, 50, 60, 50, K) + Rect(60, 50, 60, 50, Y); break;
            case "M": body = Rect(0, 0, 120, 100, B) + Line(0, 0, 120, 100, W, 26) + Line(120, 0, 0, 100, W, 26); break;
            case "N":
            {
                var sb = new StringBuilder(Rect(0, 0, 120, 100, W));
                for (var r = 0; r < 4; r++)
                    for (var c = 0; c < 4; c++)
                        if ((r + c) % 2 == 0) sb.Append(Rect(c * 30, r * 25, 30.5, 25.5, B));
                body = sb.ToString();
                break;
            }
            // Oscar: divided from upper hoist to lower fly – yellow lower-hoist triangle, red upper-fly triangle.
            case "O": body = Poly("0,0 120,0 120,100", R) + Poly("0,0 0,100 120,100", Y); break;
            case "P": body = Rect(0, 0, 120, 100, B) + Rect(38, 28, 44, 44, W); break;
            case "Q": body = Rect(0, 0, 120, 100, Y); break;
            case "R": body = Rect(0, 0, 120, 100, R) + Rect(48, 0, 24, 100, Y) + Rect(0, 38, 120, 24, Y); break;
            case "S": body = Rect(0, 0, 120, 100, W) + Rect(38, 28, 44, 44, B); break;
            case "T": body = VStripes(120, 100, R, W, B); break;
            case "U": body = Rect(0, 0, 60, 50, R) + Rect(60, 0, 60, 50, W) + Rect(0, 50, 60, 50, W) + Rect(60, 50, 60, 50, R); break;
            case "V": body = Rect(0, 0, 120, 100, W) + Line(0, 0, 120, 100, R, 22) + Line(120, 0, 0, 100, R, 22); break;
            case "W": body = Rect(0, 0, 120, 100, B) + Rect(30, 20, 60, 60, W) + Rect(46, 36, 28, 28, R); break;
            case "X": body = Rect(0, 0, 120, 100, W) + Rect(48, 0, 24, 100, B) + Rect(0, 38, 120, 24, B); break;
            case "Y":
            {
                // Ten diagonal bands rising from lower hoist to upper fly, starting with yellow at the upper-hoist corner.
                var sb = new StringBuilder(Rect(0, 0, 120, 100, Y));
                const double span = 153.6, band = span / 10;
                sb.Append("<g transform=\"rotate(-39.81 60 50)\">");
                for (var i = 1; i < 10; i += 2)
                    sb.Append(Rect(-80, 50 - span / 2 + i * band, 280, band + 0.4, R));
                sb.Append("</g>");
                body = sb.ToString();
                break;
            }
            case "Z":
                body = Poly("0,0 120,0 60,50", Y) + Poly("0,0 0,100 60,50", K) + Poly("0,100 120,100 60,50", R) + Poly("120,0 120,100 60,50", B);
                break;

            // ----- Numeral pennants (tapered) -----
            case "0": outline = PennantOutline; w = 180; body = Rect(0, 0, 180, 100, Y) + Rect(60, 0, 60, 100, R); break;
            case "1": outline = PennantOutline; w = 180; body = Rect(0, 0, 180, 100, W) + Circle(62, 50, 26, R); break;
            case "2": outline = PennantOutline; w = 180; body = Rect(0, 0, 180, 100, B) + Circle(62, 50, 26, W); break;
            case "3": outline = PennantOutline; w = 180; body = VStripes(180, 100, R, W, B); break;
            case "4": outline = PennantOutline; w = 180; body = Rect(0, 0, 180, 100, R) + Rect(38, 0, 24, 100, W) + Rect(0, 38, 180, 24, W); break;
            case "5": outline = PennantOutline; w = 180; body = VStripes(180, 100, Y, B); break;
            case "6": outline = PennantOutline; w = 180; body = HStripes(180, 100, K, W); break;
            case "7": outline = PennantOutline; w = 180; body = HStripes(180, 100, Y, R); break;
            case "8": outline = PennantOutline; w = 180; body = Rect(0, 0, 180, 100, W) + Rect(38, 0, 24, 100, R) + Rect(0, 38, 180, 24, R); break;
            case "9":
                outline = PennantOutline; w = 180;
                body = Rect(0, 0, 90, 50, W) + Rect(90, 0, 90, 50, K) + Rect(0, 50, 90, 50, R) + Rect(90, 50, 90, 50, Y);
                break;

            // ----- Code / answering pennant: five vertical stripes, red at the hoist -----
            case "ANS": outline = PennantOutline; w = 180; body = VStripes(180, 100, R, W, R, W, R); break;

            default: body = Rect(0, 0, w, h, "#888"); break;
        }
        return new Art(w, h, outline, body);
    }

    /// <summary>Full inline SVG for one flag. <paramref name="clipId"/> must be unique within the document.</summary>
    public static string Svg(string id, string clipId, int heightPx = 96)
    {
        var a = Get(id);
        var wPx = (int)Math.Round(heightPx * (double)a.W / a.H);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {a.W} {a.H}\" width=\"{wPx}\" height=\"{heightPx}\" focusable=\"false\">" +
               $"<defs><clipPath id=\"{clipId}\"><polygon points=\"{a.Outline}\"/></clipPath></defs>" +
               $"<g clip-path=\"url(#{clipId})\">{a.Body}</g>" +
               $"<polygon points=\"{a.Outline}\" fill=\"none\" stroke=\"rgba(0,0,0,.45)\" stroke-width=\"1.6\" stroke-linejoin=\"round\"/></svg>";
    }
}
