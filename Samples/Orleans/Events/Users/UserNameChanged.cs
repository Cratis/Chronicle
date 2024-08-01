using Concepts.Users;

namespace Events.Users;

[EventType]
public record UserNameChanged(UserName UserName);
