namespace Concepts.Users;

public record UserName(string Value) : ConceptAs<string>(Value)
{
    public static readonly UserName NotSet = new(string.Empty);

    public static implicit operator UserName(string value) => new(value);
}
