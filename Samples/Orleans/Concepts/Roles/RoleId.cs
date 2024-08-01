namespace Concepts.Roles;

public record RoleId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly RoleId NotSet = new(Guid.Empty);

    public static implicit operator RoleId(Guid value) => new(value);

    public static RoleId New() => new(Guid.NewGuid());
}
