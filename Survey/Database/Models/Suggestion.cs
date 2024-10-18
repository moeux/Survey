using System.ComponentModel.DataAnnotations;

namespace Survey.Database.Models;

public class Suggestion(Guid id, string name, string? note, string? iconUrl, long minimum, long maximum)
{
    public Guid Id { get; init; } = id;
    [MaxLength(256)] public string Name { get; init; } = name;

    [MaxLength(256)] public string? Note { get; init; } = note;

    [MaxLength(512)] public string? IconUrl { get; init; } = iconUrl;

    public long Minimum { get; init; } = minimum;
    public long Maximum { get; init; } = maximum;

    public override bool Equals(object? obj)
    {
        return obj is Suggestion other && (Id.Equals(other.Id) || (Name == other.Name &&
                                                                   Note == other.Note &&
                                                                   IconUrl == other.IconUrl &&
                                                                   Minimum == other.Minimum &&
                                                                   Maximum == other.Maximum));
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Note, IconUrl, Minimum, Maximum);
    }

    public static bool operator ==(Suggestion? left, Suggestion? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Suggestion? left, Suggestion? right)
    {
        return !Equals(left, right);
    }
}