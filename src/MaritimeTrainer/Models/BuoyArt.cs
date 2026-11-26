using System.Globalization;
using System.Text;

namespace MaritimeTrainer.Models;

/// <summary>Draws IALA buoys and topmarks as inline SVG.</summary>
public static class BuoyArt
{
    private const double WaterY = 172;

    private static string N(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    private static (string Path, double Top, double Left, double Right) Body(BodyShape s) => s switch
    {
        BodyShape.Can => ("M42 104 H98 V200 H42 Z", 104, 42, 98),
        BodyShape.Cone => ("M26 200 L114 200 L86 112 L54 112 Z", 112, 26, 114),
        BodyShape.Sphere => ("M70 94 A40 40 0 1 1 69.9 94 Z", 94, 30, 110),
        // pillar: flared float with a tall central tower
        _ => ("M34 200 L106 200 L90 142 L82 142 L82 64 L58 64 L58 142 L50 142 Z", 64, 52, 88),
    };

    /// <summary>Full buoy picture. <paramref name="uid"/> must be unique in the document.</summary>
    public static string Svg(MarkSpec m, string uid, int heightPx = 190)
    {
        var (path, top, left, right) = Body(m.Shape);
        var sb = new StringBuilder();
        var wPx = (int)Math.Round(heightPx * 140.0 / 230.0);
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 140 230\" width=\"{wPx}\" height=\"{heightPx}\" focusable=\"false\">");
        sb.Append($"<defs><clipPath id=\"{uid}b\"><path d=\"{path}\"/></clipPath></defs>");

        // body colours
        sb.Append($"<g clip-path=\"url(#{uid}b)\">");
        if (m.Stripes is { Length: > 0 })
        {
            var sw = (right - left) / m.Stripes.Length;
            for (var i = 0; i < m.Stripes.Length; i++)
            {
                // the outer stripes run out to the edges so a flared base is fully coloured
                var x0 = i == 0 ? 0 : left + i * sw;
                var x1 = i == m.Stripes.Length - 1 ? 140 : left + (i + 1) * sw + 0.6;
                sb.Append($"<rect x=\"{N(x0)}\" y=\"0\" width=\"{N(x1 - x0)}\" height=\"230\" fill=\"{m.Stripes[i]}\"/>");
            }
        }
        else
        {
            var total = m.Bands.Sum(b => b.Weight);
            var visible = WaterY - top;
            var y = top;
            for (var i = 0; i < m.Bands.Length; i++)
            {
                var h = visible * m.Bands[i].Weight / total;
                if (i == m.Bands.Length - 1) h = 230 - y;
                sb.Append($"<rect x=\"0\" y=\"{N(y)}\" width=\"140\" height=\"{N(h + 0.6)}\" fill=\"{m.Bands[i].Colour}\"/>");
                y += h;
            }
        }
        sb.Append("</g>");
        sb.Append($"<path d=\"{path}\" fill=\"none\" stroke=\"rgba(0,0,0,.55)\" stroke-width=\"1.8\" stroke-linejoin=\"round\"/>");

        // mast + topmark
        if (m.Top != TopmarkKind.None)
        {
            var baseY = top - 8;
            sb.Append($"<line x1=\"70\" y1=\"{N(top)}\" x2=\"70\" y2=\"{N(baseY)}\" stroke=\"#222\" stroke-width=\"3\"/>");
            sb.Append(TopmarkShapes(m.Top, m.TopColour, 70, baseY));
        }

        // sea
        sb.Append($"<path d=\"M0 {N(WaterY)} Q17 {N(WaterY - 7)} 35 {N(WaterY)} T70 {N(WaterY)} T105 {N(WaterY)} T140 {N(WaterY)} V230 H0 Z\" fill=\"#1f6a9a\" fill-opacity=\".93\"/>");
        sb.Append($"<path d=\"M0 {N(WaterY)} Q17 {N(WaterY - 7)} 35 {N(WaterY)} T70 {N(WaterY)} T105 {N(WaterY)} T140 {N(WaterY)}\" fill=\"none\" stroke=\"#a9d6f0\" stroke-width=\"2\" stroke-opacity=\".8\"/>");
        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>A topmark on its own (cropped) – used when a topmark alone is shown as a quiz prompt or answer.</summary>
    public static string TopmarkSvg(MarkSpec m, int heightPx = 110)
    {
        var wPx = (int)Math.Round(heightPx * 100.0 / 90.0);
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 90\" width=\"{wPx}\" height=\"{heightPx}\" focusable=\"false\">");
        sb.Append("<line x1=\"50\" y1=\"88\" x2=\"50\" y2=\"78\" stroke=\"#222\" stroke-width=\"4\"/>");
        sb.Append(TopmarkShapes(m.Top, m.TopColour, 50, 78));
        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string Cone(double cx, double baseY, double apexY, string fill)
    {
        var hw = 14;
        return $"<polygon points=\"{N(cx - hw)},{N(baseY)} {N(cx + hw)},{N(baseY)} {N(cx)},{N(apexY)}\" fill=\"{fill}\" stroke=\"rgba(0,0,0,.6)\" stroke-width=\"1.4\" stroke-linejoin=\"round\"/>";
    }

    private static string Ball(double cx, double cy, double r, string fill) =>
        $"<circle cx=\"{N(cx)}\" cy=\"{N(cy)}\" r=\"{N(r)}\" fill=\"{fill}\" stroke=\"rgba(0,0,0,.6)\" stroke-width=\"1.4\"/>";

    private static string TopmarkShapes(TopmarkKind kind, string fill, double cx, double by)
    {
        const double ch = 17; // cone height
        switch (kind)
        {
            case TopmarkKind.Can:
                return $"<rect x=\"{N(cx - 12)}\" y=\"{N(by - 22)}\" width=\"24\" height=\"22\" rx=\"2\" fill=\"{fill}\" stroke=\"rgba(0,0,0,.6)\" stroke-width=\"1.4\"/>";
            case TopmarkKind.Cone:
                return Cone(cx, by, by - 26, fill);
            case TopmarkKind.Sphere:
                return Ball(cx, by - 13, 13, fill);
            case TopmarkKind.TwoSpheres:
                return Ball(cx, by - 9, 9.5, fill) + Ball(cx, by - 30, 9.5, fill);
            case TopmarkKind.ConesUp:
                return Cone(cx, by, by - ch, fill) + Cone(cx, by - ch - 2, by - 2 * ch - 2, fill);
            case TopmarkKind.ConesDown:
                return Cone(cx, by - ch, by, fill) + Cone(cx, by - 2 * ch - 2, by - ch - 2, fill);
            case TopmarkKind.ConesBaseToBase:
            {
                var mid = by - ch;
                return Cone(cx, mid, by, fill) + Cone(cx, mid, mid - ch, fill);
            }
            case TopmarkKind.ConesPointToPoint:
            {
                var mid = by - ch;
                return Cone(cx, by, mid, fill) + Cone(cx, mid - ch, mid, fill);
            }
            case TopmarkKind.SaintAndrewsCross:
                return Line2(cx - 14, by - 30, cx + 14, by, fill) + Line2(cx + 14, by - 30, cx - 14, by, fill);
            case TopmarkKind.UprightCross:
                return Line2(cx, by - 32, cx, by, fill) + Line2(cx - 14, by - 20, cx + 14, by - 20, fill);
            default:
                return "";
        }
    }

    private static string Line2(double x1, double y1, double x2, double y2, string fill) =>
        $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" stroke=\"rgba(0,0,0,.65)\" stroke-width=\"9\" stroke-linecap=\"round\"/>" +
        $"<line x1=\"{N(x1)}\" y1=\"{N(y1)}\" x2=\"{N(x2)}\" y2=\"{N(y2)}\" stroke=\"{fill}\" stroke-width=\"6\" stroke-linecap=\"round\"/>";
}
