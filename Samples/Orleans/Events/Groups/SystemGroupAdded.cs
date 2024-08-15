using Concepts.Groups;

namespace Events.Groups;

[EventType]
public record SystemGroupAdded(GroupName Name);
