namespace Concepts.Users;

public record UserId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly UserId NotSet = new(Guid.Empty);

    public static implicit operator UserId(Guid value) => new(value);

    public static implicit operator EventSourceId(UserId userId) => userId.ToString();

    public static UserId New() => new(Guid.NewGuid());
}
