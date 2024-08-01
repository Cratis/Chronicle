using Cratis.Chronicle.Events;

namespace Concepts.Groups;

public record GroupId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly GroupId NotSet = new(Guid.Empty);

    public static implicit operator GroupId(Guid value) => new(value);

    public static implicit operator EventSourceId(GroupId id) => id.ToString();

    public static GroupId New() => new(Guid.NewGuid());
}
