using Cratis.Chronicle.Events;

namespace Orleans;

[EventType]
public record MyFirstEvent(string Name);
