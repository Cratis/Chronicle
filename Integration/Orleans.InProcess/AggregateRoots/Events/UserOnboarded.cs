using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Events;

[EventType]
public record UserOnBoarded(UserName Name);