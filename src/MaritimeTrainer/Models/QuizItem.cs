namespace MaritimeTrainer.Models;

/// <summary>One learnable thing (a flag/letter, a buoy, a light characteristic) with all of its representations.</summary>
public sealed class QuizItem
{
    public required string Id { get; init; }
    public required string Deck { get; init; }
    public required string Label { get; init; }
    public string Group { get; init; } = "";
    public string[] PhoneticAlts { get; init; } = [];
    public Dictionary<Rep, string> Reps { get; } = new();

    public bool Has(Rep r) => Reps.ContainsKey(r);
    public string this[Rep r] => Reps[r];
    public string? Get(Rep r) => Reps.TryGetValue(r, out var v) ? v : null;

    public QuizItem With(Rep r, string value) { Reps[r] = value; return this; }
}
