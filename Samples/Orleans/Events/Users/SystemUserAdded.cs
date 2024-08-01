using Concepts.Users;

namespace Events.Users;

[EventType]
public record SystemUserAdded(UserName UserName, ProfileName Name, UserPassword Password);