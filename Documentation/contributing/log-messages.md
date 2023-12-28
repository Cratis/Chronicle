# Log Messages

Logging can actually impact the running performance. You therefor have to use the
concept of [LoggerMessage](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-5.0).
It will generate optimized actions that can just be called.

Throughout the project we use a specific pattern where we encapsulate log messages for
a type in a class called the same as the type suffixed with `LogMessages`.

The type should be placed in a file called `<System>Logging.cs`.

It holds partial methods that represent specific log messages leveraging the [compile-time logging source generation](https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
or .NET 6. The **EventId** must be unique within every class, it should be a sequential number starting with 0.

Below is an example:

```csharp
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable MA0048 // File name must match type name

internal static partial class RoutingLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Entering routing state")]
    internal static partial void Entering(this ILogger<Routing> logger);

    [LoggerMessage(1, LogLevel.Trace, "Tail sequence numbers for observer is {TailEventSequenceNumber} and {TailEventSequenceNumberForEventTypes}")]
    internal static partial void TailEventSequenceNumbers(this ILogger<Routing> logger, EventSequenceNumber tailEventSequenceNumber, EventSequenceNumber tailEventSequenceNumberForEventTypes);

    [LoggerMessage(2, LogLevel.Trace, "Observer is not subscribed. Transitioning to disconnected state.")]
    internal static partial void NotSubscribed(this ILogger<Routing> logger);

    [LoggerMessage(3, LogLevel.Trace, "Observer is indexing. Transitioning to indexing state.")]
    internal static partial void Indexing(this ILogger<Routing> logger);

    [LoggerMessage(4, LogLevel.Trace, "Observer is catching up. Transitioning to catch up state.")]
    internal static partial void CatchingUp(this ILogger<Routing> logger);

    [LoggerMessage(5, LogLevel.Trace, "Observer is replaying. Transitioning to replay state.")]
    internal static partial void Replaying(this ILogger<Routing> logger);

    [LoggerMessage(6, LogLevel.Trace, "Observer is ready for observing. Transitioning to observing state.")]
    internal static partial void Observing(this ILogger<Routing> logger);

    [LoggerMessage(7, LogLevel.Trace, "Observer will fast forward to tail of event sequence.")]
    internal static partial void FastForwarding(this ILogger<Routing> logger);
}
```

Often at times, there is the need for common properties that adorn every log message. The .NET logging has a concept of
scopes to be able to set common properties that will within a scope. To not pollute the methods with the setup of scopes,
and at the same time allow for reuse of scopes within a module, we formalize these by adding another class to the file
called the same as the type but now with the suffix `Scopes`.

This should then hold extension methods for the different scopes one wants to formalize.

Below is an example:

```csharp
internal static class RoutingScopes
{
    internal static IDisposable? BeginRoutingScope(this ILogger<Routing> logger, ObserverSubscription subscription) =>
        logger.BeginScope(new Dictionary<string, object>
        {
            ["ObserverId"] = subscription.ObserverId,
            ["MicroserviceId"] = subscription.ObserverKey.MicroserviceId,
            ["TenantId"] = subscription.ObserverKey.TenantId,
            ["EventSequenceId"] = subscription.ObserverKey.EventSequenceId
        });
}
```

The following is an example of usage:

```csharp
public override async Task<ObserverState> OnEnter(ObserverState state)
{
    _subscription = await _observer.GetSubscription();

    // Begin the scope, will be disposed when exiting method.
    using var logScope = _logger.BeginRoutingScope(_subscription);

    _logger.Entering();

    _tailEventSequenceNumber = await _eventSequence.GetTailSequenceNumber();
    _tailEventSequenceNumberForEventTypes = await _eventSequence.GetTailSequenceNumberForEventTypes(_subscription.EventTypes);

    _logger.TailEventSequenceNumbers(_tailEventSequenceNumber, _tailEventSequenceNumberForEventTypes);

    return await EvaluateState(state);
}
```

