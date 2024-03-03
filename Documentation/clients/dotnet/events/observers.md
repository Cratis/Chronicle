# Observers

Observers are classes that holds methods that react to certain events.
By adding an attribute in front of the handler class, it will automatically be discovered at startup and
the system will figure out which methods will be handling what events.

```csharp
using Cratis.Observation;

[Observer("a5a55a35-1846-4386-b752-4d9a3da7aa10")]
public class MyObserver
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

## Middlewares

If you want to get a callback for every handle method that gets called, you can implement what is known as a **Event Handler Middleware**.
They are discovered automatically from their interface type and configured at startup.

For instance, lets say you want to create a middleware that logs something before and after handle:

```csharp
using Cratis.Events;
using Cratis.Observation;

public class MyObserverMiddleware : IObserverMiddleware
{
    readonly ILogger<MyObserverMiddleware> _logger;

    public MyObserverMiddleware(ILogger<MyObserverMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task Invoke(EventContext eventContext, object @event, NextEventHandlerMiddleware next)
    {
        var before = DateTime.UtcNow;
        await next();
        var after = DateTime.UtcNow;
        var delta = after.Subtract(before);
        _logger.LogInformation("It took {time} milliseconds to run the event handler for type {event}", delta.TotalMilliseconds, @event.GetType().Name);
    }
}
```
