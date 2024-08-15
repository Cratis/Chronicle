using Concepts.Roles;

namespace Events.Groups;

[EventType]
public record RoleRemovedFromGroup(RoleId RoleId);
