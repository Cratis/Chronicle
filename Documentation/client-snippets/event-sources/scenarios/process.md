```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventType]
public record TransferRequested(string From, string To, decimal Amount);

[EventType]
public record TransferFundsReserved;

[EventType]
[ClosesStream]
public record TransferCompleted;

// One event source per transfer, and one stream for the process that carries it out.
[EventSource("Transfer", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Process", Description = "The steps of one transfer")]
public class TransferEventSource : IEventSource;

// Only the steps of a transfer's own process reach this reactor.
[FromEventSource<TransferEventSource>("Process")]
public class TransferProcess(IEventLog eventLog) : IReactor
{
    public async Task Requested(TransferRequested @event, EventContext context)
    {
        // Reserve the funds, then record the step on the same stream.
        await eventLog.Append<TransferEventSource>(context.EventSourceId, new TransferFundsReserved(), eventStream: "Process");
    }

    public async Task Reserved(TransferFundsReserved @event, EventContext context)
    {
        // The closing event ends the stream: nothing more can be appended to it.
        await eventLog.Append<TransferEventSource>(context.EventSourceId, new TransferCompleted(), eventStream: "Process");
    }
}

public class TransferStarter(IEventLog eventLog)
{
    public Task<AppendResult> Start(EventSourceId transferId, string from, string to, decimal amount) =>
        eventLog.Append<TransferEventSource>(transferId, new TransferRequested(from, to, amount), eventStream: "Process");
}
```
