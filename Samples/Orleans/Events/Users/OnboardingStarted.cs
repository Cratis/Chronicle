using Concepts.Users;
using Cratis.Chronicle.Events.Constraints;

namespace Events.Users;

[EventType]
[Unique]
public record OnboardingStarted(UserName UserName, ProfileName Name, UserPassword Password);
