# Reactors

Reactors are classes that holds methods that react to certain events.
By implementing the interface `IReactor`, it will automatically be discovered at startup and
the system will figure out which methods will be handling what events by convention.

```csharp
using Cratis.Chronicle.Reactors;

public class MyReactor : IReactor
{
}
```

The methods you add onto the class will be discovered by convention and the system recognizes the following
signatures, name of the method(s) can be anything:

```csharp
public void SynchronousMethodWithoutContext(MyEvent @event);
public void SynchronousMethodWithContext(MyEvent @event, EventContext context);
public Task AsynchronousMethodWithoutContext(MyEvent @event);
public Task AsynchronousMethodWithContext(MyEvent @event, EventContext context);
```

> Note: Only public methods are supported without any return types. Also worth noting is that you can't have
> two methods handling the same event as the system would not know how to recover if one of them fails.
