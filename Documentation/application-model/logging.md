# Logging

Logging can actually impact the running performance. You therefor have to use the
concept of [LoggerMessage](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-5.0).
It will generate optimized actions that can just be called.

A good practice would be to have a class called the same as the class you're logging for suffixed with `LogMessages`.
This then holds partial methods that represent specific log messages leveraging the [compile-time logging source generation](https://docs.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator)
or .NET 6. The **EventId** must be unique within every class, its a sequential number starting with 0.

Below is an example:

```csharp
    public static class EventLogLoggerMessages
    {
        [LoggerMessage(0, LogLevel.Information, "Committing event with '{SequenceNumber}' as sequence number")]
        internal static partial void Committing(this ILogger logger, EventType eventType, EventSourceId eventSource, uint sequenceNumber, EventLogId eventLog);

        [LoggerMessage(1, LogLevel.Error, "Problem committing event to storage")]
        internal static partial void CommitFailure(this ILogger logger, Exception exception);
    }
```
