// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_delivering_concurrently.given;

public class a_reducer_with_gated_partitions : for_Reducers.given.all_dependencies
{
    protected static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    protected readonly ConcurrentQueue<string> _log = new();
    protected readonly ConcurrentQueue<ReducerResult> _results = new();
    protected Subject<ReduceOperationMessage> _observations;
    System.Diagnostics.ActivitySource _traceSource;
    readonly ConcurrentDictionary<string, TaskCompletionSource> _started = new();
    readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
    readonly ConcurrentDictionary<ulong, TaskCompletionSource<ReducerResult>> _answered = new();

    protected override int MaxConcurrentPartitions => 2;

    async Task Establish()
    {
        _traceSource = new System.Diagnostics.ActivitySource("concurrent-reducer-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reducers>(_traceSource);
        _eventStore.Connection.Lifecycle.ConnectionId.Returns(Connections.ConnectionId.New());
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(OrderPlaced));
        var handler = Substitute.For<IReducerHandler>();
        handler.ReducerType.Returns(typeof(object));
        handler.ReadModelType.Returns(typeof(Order));
        handler.Id.Returns((ReducerId)"concurrent");
        handler.EventSequenceId.Returns(EventSequences.EventSequenceId.Log);
        handler.IsActive.Returns(true);
        handler.EventTypes.Returns([new EventType("order-placed", 1)]);
        handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
            .Returns(call => Reduce(call.Arg<IEnumerable<AppendedEvent>>().Single()));
        _handlersByModelType[typeof(Order)] = handler;
        _observations = new();
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Subscribe(message =>
            {
                if (message.Content.Value is ReducerResult result)
                {
                    _results.Enqueue(result);
                    AnsweredFor(result.DeliveryId).TrySetResult(result);
                }
            });
            return _observations;
        });
        _reducers = CreateReducers();
        await _reducers.Register();
    }

    void Destroy()
    {
        foreach (var gate in _gates.Values)
        {
            gate.TrySetResult();
        }

        _observations.Dispose();
        _traceSource.Dispose();
    }

    protected void Deliver(string partition, ulong sequenceNumber, ulong deliveryId) => _observations.OnNext(new ReduceOperationMessage
    {
        Partition = partition,
        DeliveryId = deliveryId,
        Events = [new Contracts.Events.AppendedEvent
        {
            Context = (EventContext.Empty with
            {
                EventSourceId = partition,
                EventType = new EventType("order-placed", 1),
                SequenceNumber = sequenceNumber
            }).ToContract(),
            Content = "{}"
        }]
    });

    protected Task Started(string partition) => StartedFor(partition).Task.WaitAsync(_deadline);

    protected void Release(string partition) => GateFor(partition).TrySetResult();

    protected Task<ReducerResult> Answered(ulong deliveryId) => AnsweredFor(deliveryId).Task.WaitAsync(_deadline);

    protected bool IsAnswered(ulong deliveryId) => AnsweredFor(deliveryId).Task.IsCompleted;

    protected int IndexOf(string entry) => Array.IndexOf(_log.ToArray(), entry);

    async Task<ReduceResult> Reduce(AppendedEvent @event)
    {
        var partition = @event.Context.EventSourceId.Value;
        _log.Enqueue($"{partition}:start");
        StartedFor(partition).TrySetResult();
        try
        {
            await GateFor(partition).Task;
        }
        finally
        {
            _log.Enqueue($"{partition}:end");
        }

        return new ReduceResult(null, @event.Context.SequenceNumber, [], string.Empty);
    }

    TaskCompletionSource StartedFor(string partition) => _started.GetOrAdd(partition, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    TaskCompletionSource GateFor(string partition) => _gates.GetOrAdd(partition, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    TaskCompletionSource<ReducerResult> AnsweredFor(ulong deliveryId) => _answered.GetOrAdd(deliveryId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    public record OrderPlaced;

    public record Order;
}
