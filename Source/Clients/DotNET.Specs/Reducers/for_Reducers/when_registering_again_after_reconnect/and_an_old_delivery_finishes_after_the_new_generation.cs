// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_again_after_reconnect;

public class and_an_old_delivery_finishes_after_the_new_generation : given.a_connected_reducers
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);
    readonly TaskCompletionSource _oldStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _releaseOld = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ConcurrentDictionary<ulong, TaskCompletionSource<ReducerResult>> _answers = new();
    readonly List<Subject<ReduceOperationMessage>> _streams = [];
    System.Diagnostics.ActivitySource _traceSource;
    ReducerResult _result;

    async Task Establish()
    {
        _traceSource = new("reconnecting-reducer-cache-specification");
        _activitySource = new Cratis.Traces.ActivitySource<Reducers>(_traceSource);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _eventTypes.GetClrTypeFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(typeof(Changed));
        var reducer = new GatedReducer(_oldStarted, _releaseOld.Task);
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(GatedReducer))
            .Returns(_ => new ActivatedArtifact(reducer, typeof(GatedReducer), NullLogger<ActivatedArtifact>.Instance));
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            call.Arg<IObservable<ReducerMessage>>().Subscribe(message =>
            {
                if (message.Content.Value is ReducerResult result)
                {
                    AnswerFor(result.DeliveryId).TrySetResult(result);
                }
            });
            var stream = new Subject<ReduceOperationMessage>();
            _streams.Add(stream);
            return stream;
        });
        _reducers = CreateReducers();
        _eventStore.Reducers.Returns(_reducers);
        await _reducers.Register(typeof(GatedReducer));
    }

    async Task Because()
    {
        _streams[0].OnNext(Operation(1, 0));
        await _oldStarted.Task.WaitAsync(_deadline);

        _lifecycle.OnDisconnected += Raise.Event<Disconnected>();
        _lifecycle.ConnectionId.Returns(ConnectionId.New());
        await _reducers.RegisterAgainAfterReconnect();

        _streams[1].OnNext(Operation(2, 10));
        await AnswerFor(2).Task.WaitAsync(_deadline);
        _releaseOld.TrySetResult();
        await AnswerFor(1).Task.WaitAsync(_deadline);

        var unchanged = Operation(3, 0);
        unchanged.InitialState = null;
        unchanged.InitialStateUnchanged = true;
        _streams[1].OnNext(unchanged);
        _result = await AnswerFor(3).Task.WaitAsync(_deadline);
    }

    [Fact] void should_succeed_using_the_new_generations_cached_state() => _result.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_not_lose_the_new_generations_updates() => JsonSerializer.Deserialize<Model>(_result.ReadModelState)!.Count.ShouldEqual(12);

    void Destroy()
    {
        _releaseOld.TrySetResult();
        foreach (var stream in _streams)
        {
            stream.Dispose();
        }
        _traceSource.Dispose();
    }

    TaskCompletionSource<ReducerResult> AnswerFor(ulong deliveryId) => _answers.GetOrAdd(deliveryId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    static ReduceOperationMessage Operation(ulong deliveryId, int count) => new()
    {
        Partition = "partition",
        DeliveryId = deliveryId,
        InitialState = JsonSerializer.Serialize(new Model(count)),
        Events = [new Contracts.Events.AppendedEvent
        {
            Context = (EventContext.Empty with
            {
                EventSourceId = "partition",
                EventType = new("changed", 1),
                SequenceNumber = deliveryId
            }).ToContract(),
            Content = "{}"
        }]
    };

    public record Model(int Count);

    public class GatedReducer(TaskCompletionSource oldStarted, Task releaseOld) : IReducerFor<Model>
    {
        public async Task<Model> Reduce(Changed @event, Model? current, EventContext context)
        {
            if (context.SequenceNumber == 1)
            {
                oldStarted.TrySetResult();
                await releaseOld;
            }
            return new((current?.Count ?? 0) + 1);
        }
    }
}
