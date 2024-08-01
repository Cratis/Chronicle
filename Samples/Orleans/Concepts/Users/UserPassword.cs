namespace Concepts.Users;

public record UserPassword(string Value) : ConceptAs<string>(Value)
{
    public static readonly UserPassword NotSet = new(string.Empty);

    public static implicit operator UserPassword(string value) => new(value);
}