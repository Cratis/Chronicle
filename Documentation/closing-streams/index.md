# Closing Streams

Event streams can be permanently closed to prevent further appends. Once a stream is closed, any attempt to append events to it will result in a constraint violation of type `StreamClosed`.

## Why close a stream?

Closing a stream is useful when a logical unit of work is complete and no further events should be recorded for that stream. Examples include:

- Finalizing an invoice — once issued, no further line items should be added.
- Archiving a case — the case is resolved and the event history is sealed.
- Completing an order — the order lifecycle has ended and further mutations are disallowed.

## How to close a stream

Call `CompleteStream` on the event log with the stream type and stream identifier you want to close:

```csharp
var result = await eventLog.CompleteStream(
    new EventStreamType("invoices"),
    new EventStreamId("invoice-42"));

result.Match(
    sequenceNumber => Console.WriteLine($"Stream closed at sequence number {sequenceNumber}"),
    error => Console.WriteLine($"Failed to close stream: {error}"));
```

The method returns `Result<EventSequenceNumber, CompleteStreamError>`.

## Error cases

| Error | Meaning |
|---|---|
| `AlreadyCompleted` | The stream has already been closed. |
| `DefaultStreamCannotBeCompleted` | The default stream (`EventStreamType.All` / `EventStreamId.Default`) cannot be closed. |

## What happens after closing

After a stream is closed, any append targeting that stream is rejected with a `StreamClosed` constraint violation:

```csharp
var appendResult = await eventLog.Append(
    eventSourceId,
    new SomeEvent("data"),
    new EventStreamType("invoices"),
    new EventStreamId("invoice-42"));

if (!appendResult.IsSuccess)
{
    var violation = appendResult.ConstraintViolations
        .FirstOrDefault(v => v.ConstraintType == ConstraintType.StreamClosed);
    // violation is non-null; stream is closed
}
```

The rejection is enforced by the `ClosedStreamConstraintValidator` which is automatically active for every event sequence — no additional configuration is required.

## Checking whether a stream is closed

You can query the current status of a stream at any time:

```csharp
bool isClosed = await eventLog.IsStreamCompleted(
    new EventStreamType("invoices"),
    new EventStreamId("invoice-42"));
```
