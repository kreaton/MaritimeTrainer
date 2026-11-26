using MaritimeTrainer.Models;

namespace MaritimeTrainer.Services;

public sealed class Question
{
    public required QuizItem Item { get; init; }
    public required Rep Prompt { get; init; }
    public required Rep Answer { get; init; }
    /// <summary>The options shown in multiple-choice mode (includes <see cref="Item"/>).</summary>
    public required IReadOnlyList<QuizItem> Choices { get; init; }
    public int CorrectIndex { get; init; }
    public bool Typed { get; init; }
}

/// <summary>Builds questions "show representation X, give representation Y" for any deck, favouring weaker items.</summary>
public sealed class QuizEngine
{
    private readonly IReadOnlyList<QuizItem> _items;
    private readonly ProgressService _progress;
    private readonly Random _rng;

    public QuizEngine(IReadOnlyList<QuizItem> items, ProgressService progress, Random? rng = null)
    {
        _items = items;
        _progress = progress;
        _rng = rng ?? Random.Shared;
    }

    public IReadOnlyList<QuizItem> Items => _items;

    /// <summary>All (prompt, answer) pairs that at least two items can support.</summary>
    public List<(Rep Prompt, Rep Answer)> ValidPairs(IEnumerable<Rep> reps, bool typed)
    {
        var list = reps.ToList();
        var pairs = new List<(Rep, Rep)>();
        foreach (var p in list)
            foreach (var a in list)
            {
                if (p == a) continue;
                if (typed && !RepInfo.IsTypable(a)) continue;
                // two Morse renderings of the same thing make a poor "typed" question
                if (typed && RepInfo.IsMorse(p) && RepInfo.IsMorse(a)) continue;
                if (_items.Count(i => i.Has(p) && i.Has(a)) >= 2) pairs.Add((p, a));
            }
        return pairs;
    }

    public double Weight(QuizItem item)
    {
        var s = _progress.Get(item.Id);
        if (s.Attempts == 0) return 3.0;
        return 0.4 + 3.0 * (1 - s.Accuracy) + 2.0 / (1 + s.Streak);
    }

    public QuizItem? PickWeighted(IReadOnlyList<QuizItem> pool, string? avoidId = null)
    {
        if (pool.Count == 0) return null;
        var candidates = pool.Count > 1 && avoidId is not null ? pool.Where(i => i.Id != avoidId).ToList() : pool.ToList();
        var weights = candidates.Select(Weight).ToArray();
        return candidates[PickIndex(weights)];
    }

    private int PickIndex(double[] weights)
    {
        var total = weights.Sum();
        var r = _rng.NextDouble() * total;
        for (var i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r <= 0) return i;
        }
        return weights.Length - 1;
    }

    /// <summary>
    /// Creates the next question. Pass <paramref name="prompt"/> and <paramref name="answer"/> for a fixed direction,
    /// or null for both to mix every direction the deck supports.
    /// </summary>
    public Question? Next(IEnumerable<Rep> deckReps, Rep? prompt, Rep? answer, bool typed, string? avoidId = null, int choiceCount = 4)
    {
        Rep p, a;
        if (prompt is null || answer is null)
        {
            var pairs = ValidPairs(deckReps, typed);
            if (pairs.Count == 0) return null;
            (p, a) = pairs[_rng.Next(pairs.Count)];
        }
        else
        {
            (p, a) = (prompt.Value, answer.Value);
        }

        var pool = _items.Where(i => i.Has(p) && i.Has(a)).ToList();
        if (pool.Count < 2) return null;
        var item = PickWeighted(pool, avoidId);
        if (item is null) return null;

        var answerPool = _items.Where(i => i.Has(a)).ToList();
        var distractors = PickDistractors(item, answerPool, a, Math.Max(1, choiceCount - 1));
        var choices = distractors.Append(item).OrderBy(_ => _rng.Next()).ToList();

        return new Question
        {
            Item = item,
            Prompt = p,
            Answer = a,
            Choices = choices,
            CorrectIndex = choices.IndexOf(item),
            Typed = typed,
        };
    }

    private List<QuizItem> PickDistractors(QuizItem correct, List<QuizItem> pool, Rep answerRep, int count)
    {
        var correctValue = correct[answerRep];
        var seen = new HashSet<string> { correctValue };
        var cands = new List<QuizItem>();
        foreach (var c in pool.OrderBy(_ => _rng.Next()))
            if (c.Id != correct.Id && seen.Add(c[answerRep])) cands.Add(c);

        var chosen = new List<QuizItem>();
        while (chosen.Count < count && cands.Count > 0)
        {
            var weights = cands.Select(c => 1.0 + Similarity(correct, c)).ToArray();
            var idx = PickIndex(weights);
            chosen.Add(cands[idx]);
            cands.RemoveAt(idx);
        }
        return chosen;
    }

    /// <summary>How easily one item is confused with another – used to make distractors plausible.</summary>
    private static double Similarity(QuizItem a, QuizItem b)
    {
        var s = 0.0;
        if (a.Has(Rep.MorseVisual) && b.Has(Rep.MorseVisual))
        {
            var d = Morse.Distance(a[Rep.MorseVisual], b[Rep.MorseVisual]);
            s += Math.Max(0, 3 - d);
        }
        if (a.Group.Length > 0 && a.Group == b.Group && a.Deck != "signals") s += 1.5;
        return s;
    }

    // ---- typed answers ----

    private static string Squash(string s) =>
        new(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    public static bool Verify(Question q, string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        var item = q.Item;
        switch (q.Answer)
        {
            case Rep.Symbol:
                return Squash(input) == Squash(item[Rep.Symbol]);
            case Rep.Phonetic:
            {
                var x = Squash(input);
                if (x == Squash(item[Rep.Phonetic])) return true;
                return item.PhoneticAlts.Any(alt => Squash(alt) == x);
            }
            case Rep.MorseAudio:
            case Rep.MorseVisual:
                return Morse.Normalize(input) == item[q.Answer];
            default:
                return false;
        }
    }
}
