using Concepts.Users;

namespace Events.Users;

[EventType]
public record ProfileNameChanged(ProfileName Name);
