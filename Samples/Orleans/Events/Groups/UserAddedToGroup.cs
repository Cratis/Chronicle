using Concepts.Users;

namespace Events.Groups;

[EventType]
public record UserAddedToGroup(UserId UserId);