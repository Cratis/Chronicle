using Cratis.Chronicle.Events;

namespace Orleans;

[EventType("b80f141b-54d2-4441-88dd-1364161c5bd6")]
public record MyFirstEvent(string Name);
