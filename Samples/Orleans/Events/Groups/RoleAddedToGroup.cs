using Concepts.Roles;

namespace Events.Groups;

[EventType]
public record RoleAddedToGroup(RoleId RoleId);
