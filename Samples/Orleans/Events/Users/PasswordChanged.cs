using Concepts.Users;

namespace Events.Users;

[EventType]
public record PasswordChanged(UserPassword Password);
