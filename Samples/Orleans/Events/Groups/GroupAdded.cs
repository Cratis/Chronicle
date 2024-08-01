using Concepts.Groups;

namespace Events.Groups;

[EventType]
public record GroupAdded(GroupName Name);
