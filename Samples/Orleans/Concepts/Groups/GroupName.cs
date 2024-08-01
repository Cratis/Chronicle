namespace Concepts.Groups;

public record GroupName(string Value) : ConceptAs<string>(Value)
{
    public static readonly GroupName NotSet = new(string.Empty);

    public static implicit operator GroupName(string value) => new(value);
}