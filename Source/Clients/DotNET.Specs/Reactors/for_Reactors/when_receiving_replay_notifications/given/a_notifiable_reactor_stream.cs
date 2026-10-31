// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Observation;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_replay_notifications.given;

public class a_notifiable_reactor_stream : an_explicit_store
{
    protected ReplayWork _work;
    readonly Subject<EventsToObserve> _observed = new();
    readonly ConcurrentDictionary<ulong, TaskCompletionSource<ReactorResult>> _answers = new();
    System.Diagnostics.ActivitySource _trace;
    IDisposable _messages;
    IReactorHandler _handler;

    async Task Establish()
    {
        await _provider.DisposeAsync();
        _work = new();
        _trace = new("reactor-replay-ordering");
        var services = new ServiceCollection().AddSingleton(_work);
        services.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        services.AddKeyedSingleton<IActivitySource<Reactors>>(ClientActivity.SourceName, (_, _) => new Cratis.Traces.ActivitySource<Reactors>(_trace));
        services.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = services.BuildServiceProvider();
        _activator = new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance);
        _artifacts.Reactors.Returns([typeof(NotifiableReactor)]);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("replay-event"));
        _options.Reactors.MaxConcurrentPartitions = 2;
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _messages = call.Arg<IObservable<ReactorMessage>>().Subscribe(message =>
            {
                if (message.Content.Value is ReactorResult result) AnswerFor(result.DeliveryId).TrySetResult(result);
            });
            return _observed;
        });
        _store = CreateStore();
        await _store.DiscoverAll().WaitAsync(TimeSpan.FromSeconds(10));
        await _store.Reactors.Register().WaitAsync(TimeSpan.FromSeconds(10));
        _handler = _store.Reactors.GetHandlerFor<NotifiableReactor>();
    }

    async Task Destroy()
    {
        _work.ReleaseAll();
        try
        {
            await Task.WhenAll(_answers.Values.Select(answer => answer.Task)).WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            _handler?.Disconnect();
            (_handler as IDisposable)?.Dispose();
            _messages?.Dispose();
            _observed.Dispose();
            _trace.Dispose();
        }
    }

    protected void Deliver(string partition, string name, ulong deliveryId)
    {
        AnswerFor(deliveryId);
        _observed.OnNext(new EventsToObserve
        {
            Partition = partition,
            DeliveryId = deliveryId,
            Events = [new Contracts.Events.AppendedEvent
            {
                Context = (EventContext.Empty with
                {
                    EventSourceId = partition,
                    EventType = new("replay-event", 1),
                    SequenceNumber = deliveryId
                }).ToContract(),
                Content = JsonSerializer.Serialize(new PlainEvent(name))
            }]
        });
    }

    protected void Notify(ReplayState state, string partition = "") =>
        _observed.OnNext(new EventsToObserve { ReplayState = state, Partition = partition });

    protected Task<ReactorResult> Answered(ulong deliveryId) => AnswerFor(deliveryId).Task.WaitAsync(TimeSpan.FromSeconds(10));

    TaskCompletionSource<ReactorResult> AnswerFor(ulong deliveryId) =>
        _answers.GetOrAdd(deliveryId, _ => new(TaskCreationOptions.RunContinuationsAsynchronously));

    public class NotifiableReactor(ReplayWork work) : IReactor, ICanBeNotifiedWhenReplay, ICanBeNotifiedWhenPartitionReplayed
    {
        public Task On(PlainEvent @event) => work.Run(@event.Name);
        public Task BeginReplay() => work.Run("begin");
        public Task EndReplay() => work.Run("end");
        public Task BeginReplayPartition(Partition partition) => work.Run($"begin:{partition.Value}");
        public Task EndReplayPartition(Partition partition) => work.Run($"end:{partition.Value}");
    }
}
