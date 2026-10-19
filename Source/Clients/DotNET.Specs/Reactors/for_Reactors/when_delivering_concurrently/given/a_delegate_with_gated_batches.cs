// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

using ContractAppendedEvent = Cratis.Chronicle.Contracts.Events.AppendedEvent;
using ContractReactors = Cratis.Chronicle.Contracts.Observation.Reactors.IReactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_delivering_concurrently.given;

public class a_delegate_with_gated_batches : for_Reactors.given.all_dependencies
{
    protected static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    protected readonly ConcurrentQueue<string> _log = new();
    protected readonly ConcurrentQueue<ReactorResult> _results = new();
    protected Subject<EventsToObserve> _observed;
    readonly ConcurrentDictionary<string, TaskCompletionSource> _started = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource<ReactorResult>> _answered = new();

    protected override int MaxConcurrentPartitions => 2;

    async Task Establish()
    {
        _observed = new Subject<EventsToObserve>();
        var reactors = Substitute.For<ContractReactors>();
        _services.Reactors.Returns(reactors);
        reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>())
            .Returns(call =>
            {
                call.Arg<IObservable<ReactorMessage>>().Subscribe(message =>
                {
                    if (message.Content.Value is ReactorResult result)
                    {
                        OnResult(result);
                        _results.Enqueue(result);
                        AnsweredFor(result.DeliveryId).TrySetResult(result);
                    }
                });
                return _observed;
            });
        await _reactors.Register(
            "concurrent",
            builder => builder.WithEventType(new EventType("orders", 1)),
            async (@event, _) =>
            {
                var name = Name(@event.Context.EventSourceId.Value, @event.Context.SequenceNumber.Value);
                _log.Enqueue($"{name}:start");
                StartedFor(name).TrySetResult();
                try
                {
                    await GateFor(name).Task;
                }
                finally
                {
                    _log.Enqueue($"{name}:end");
                    OnHandled(name);
                }
            });
    }

    void Destroy()
    {
        foreach (var gate in _gates.Values)
        {
            gate.TrySetResult();
        }

        _observed.Dispose();
    }

    protected virtual void OnResult(ReactorResult result)
    {
    }

    protected virtual void OnHandled(string name)
    {
    }

    protected void Deliver(string partition, ulong sequenceNumber, ulong deliveryId) => _observed.OnNext(new EventsToObserve
    {
        Partition = partition,
        DeliveryId = deliveryId,
        Events = [new ContractAppendedEvent
        {
            Context = (EventContext.Empty with
            {
                EventSourceId = partition,
                EventType = new EventType("orders", 1),
                SequenceNumber = sequenceNumber
            }).ToContract(),
            Content = "{}"
        }]
    });

    protected void DeliverReplayState(ReplayState replayState, string partition) => _observed.OnNext(new EventsToObserve
    {
        Partition = partition,
        ReplayState = replayState
    });

    protected Task Started(string partition, ulong sequenceNumber) => StartedFor(Name(partition, sequenceNumber)).Task.WaitAsync(_deadline);

    protected void Release(string partition, ulong sequenceNumber) => GateFor(Name(partition, sequenceNumber)).TrySetResult();

    protected Task<ReactorResult> Answered(ulong deliveryId) => AnsweredFor(deliveryId).Task.WaitAsync(_deadline);

    protected bool IsAnswered(ulong deliveryId) => AnsweredFor(deliveryId).Task.IsCompleted;

    protected int IndexOf(string entry) => Array.IndexOf(_log.ToArray(), entry);

    static string Name(string partition, ulong sequenceNumber) => $"{partition}:{sequenceNumber}";

    TaskCompletionSource StartedFor(string name) => _started.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    TaskCompletionSource GateFor(string name) => _gates.GetOrAdd(name, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    TaskCompletionSource<ReactorResult> AnsweredFor(ulong deliveryId) => _answered.GetOrAdd($"{deliveryId}", _ => new(TaskCreationOptions.RunContinuationsAsynchronously));
}
