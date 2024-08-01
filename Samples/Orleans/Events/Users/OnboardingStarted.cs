using Concepts.Users;

namespace Events.Users;

[EventType]
public record OnboardingStarted(UserName UserName, ProfileName Name, UserPassword Password);
