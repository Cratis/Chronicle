namespace Concepts.Users;

public record ProfileName(string Value) : ConceptAs<string>(Value)
{
    public static readonly ProfileName NotSet = new(string.Empty);

    public static implicit operator ProfileName(string value) => new(value);
}
