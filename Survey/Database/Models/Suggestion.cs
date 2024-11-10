using System.ComponentModel.DataAnnotations;

namespace Survey.Database.Models;

public class Suggestion(string name, string? note, string? iconUrl, long minimum, long maximum, ulong userId)
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
    [MaxLength(256)] public string Name { get; init; } = name;

    [MaxLength(256)] public string? Note { get; init; } = note;

    [MaxLength(512)] public string? IconUrl { get; init; } = iconUrl;

    public long Minimum { get; init; } = minimum;
    public long Maximum { get; init; } = maximum;

    public ulong UserId { get; init; } = userId;

    public override bool Equals(object? obj)
    {
        return obj is Suggestion other &&
               (Id.Equals(other.Id) || (string.Equals(Name, other.Name, StringComparison.InvariantCultureIgnoreCase) &&
                                        string.Equals(Note, other.Note, StringComparison.InvariantCultureIgnoreCase) &&
                                        Minimum == other.Minimum &&
                                        Maximum == other.Maximum));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Note, IconUrl, UserId, Minimum, Maximum);
    }
}