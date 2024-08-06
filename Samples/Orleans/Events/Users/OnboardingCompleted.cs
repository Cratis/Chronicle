using Cratis.Chronicle.Events.Constraints;

namespace Events.Users;

[EventType]
[Unique]
public record OnboardingCompleted();
