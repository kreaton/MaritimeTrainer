using System.Text;

namespace MaritimeTrainer.Models;

public static class Morse
{
    private static readonly Dictionary<char, string> Table = new()
    {
        ['A'] = ".-",   ['B'] = "-...", ['C'] = "-.-.", ['D'] = "-..",  ['E'] = ".",
        ['F'] = "..-.", ['G'] = "--.",  ['H'] = "....", ['I'] = "..",   ['J'] = ".---",
        ['K'] = "-.-",  ['L'] = ".-..", ['M'] = "--",   ['N'] = "-.",   ['O'] = "---",
        ['P'] = ".--.", ['Q'] = "--.-", ['R'] = ".-.",  ['S'] = "...",  ['T'] = "-",
        ['U'] = "..-",  ['V'] = "...-", ['W'] = ".--",  ['X'] = "-..-", ['Y'] = "-.--",
        ['Z'] = "--..",
        ['0'] = "-----", ['1'] = ".----", ['2'] = "..---", ['3'] = "...--", ['4'] = "....-",
        ['5'] = ".....", ['6'] = "-....", ['7'] = "--...", ['8'] = "---..", ['9'] = "----.",
    };

    public static IReadOnlyDictionary<char, string> All => Table;

    public static string? For(char c) => Table.TryGetValue(char.ToUpperInvariant(c), out var m) ? m : null;

    /// <summary>
    /// Encodes text into a pattern the audio engine understands: '.' and '-' are elements,
    /// ' ' separates letters and '/' separates words. Unsupported characters are dropped.
    /// </summary>
    public static string Encode(string text)
    {
        var words = new List<string>();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var letters = word.Select(For).Where(m => m is not null);
            var enc = string.Join(' ', letters);
            if (enc.Length > 0) words.Add(enc);
        }
        return string.Join('/', words);
    }

    /// <summary>Accepts user input such as "·–·" or ". - ." and returns a clean ".-." pattern.</summary>
    public static string Normalize(string input)
    {
        var sb = new StringBuilder();
        foreach (var ch in input)
        {
            switch (ch)
            {
                case '.': case '·': case '•': case '∙': sb.Append('.'); break;
                case '-': case '–': case '—': case '−': case '_': sb.Append('-'); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>Levenshtein distance between two Morse patterns (used to pick confusable distractors).</summary>
    public static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                                   d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
        return d[a.Length, b.Length];
    }

    /// <summary>Common procedural signals worth knowing alongside the alphabet.</summary>
    public static readonly (string Name, string Code, string Meaning)[] Prosigns =
    [
        ("SOS", "...---...", "International distress signal (sent as one continuous group)"),
        ("AR", ".-.-.", "End of message"),
        ("AS", ".-...", "Wait"),
        ("BT", "-...-", "Break / new paragraph"),
        ("K", "-.-", "Go ahead – invitation to transmit"),
        ("R", ".-.", "Received / understood"),
        ("SK", "...-.-", "End of contact"),
        ("Error", "........", "Error – I am correcting my last word (8 dots)"),
        ("CQ", "-.-. --.-", "General call to all stations"),
    ];
}
