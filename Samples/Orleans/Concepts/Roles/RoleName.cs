namespace Concepts.Roles;

public record RoleName(string Value) : ConceptAs<string>(Value)
{
    public static readonly RoleName NotSet = new(string.Empty);

    public static implicit operator RoleName(string value) => new(value);
}
