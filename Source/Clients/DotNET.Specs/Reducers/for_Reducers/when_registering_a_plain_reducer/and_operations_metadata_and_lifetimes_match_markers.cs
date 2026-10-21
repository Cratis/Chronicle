// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Registrations.for_PlainObserverParity.given;
using Cratis.Serialization;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_registering_a_plain_reducer;

public class and_operations_metadata_and_lifetimes_match_markers : an_explicit_store
{
    readonly Dictionary<string, Subject<ReduceOperationMessage>> _streams = [];
    readonly Dictionary<string, ReducerDefinition> _definitions = [];
    readonly Type _markedType = typeof(MarkedReducer);
    ObserverLifecycle.Ledger _ledger;
    System.Diagnostics.ActivitySource _trace;
    IReducerObservers _observers;
    Reducers _reducers;
    IReducerHandler _plain;
    IReducerHandler _marked;
    IReducerHandler _runtime;
    JobId[] _jobs;
    Guid _job;

    void Establish()
    {
        _provider.Dispose();
        _ledger = new();
        _trace = new("reducer-parity");
        var services = new ServiceCollection().AddSingleton(_ledger);
        services.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        services.AddKeyedSingleton<IActivitySource<Reactors.Reactors>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reactors.Reactors>>());
        services.AddKeyedSingleton<IActivitySource<Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers>>());
        _provider = services.BuildServiceProvider();
        _activator = new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance);
        _artifacts.Reducers.Returns([typeof(MarkedReducer), typeof(MarkedPassiveReducer)]);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias"))
            .RegisterReducer<PlainReducer>("plain-alias").RegisterReducer<PlainPassiveReducer>("plain-passive");
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            string id = null!;
            call.Arg<IObservable<ReducerMessage>>().Take(1).Subscribe(message =>
            {
                var definition = message.Content.Value0!.Reducer;
                id = definition.ReducerId;
                _definitions[id] = definition;
                _streams[id] = new();
            });
            return _streams[id];
        });
        _job = Guid.NewGuid();
        _services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>()).Returns(new ObserverInformation());
        _services.Observers.Replay(Arg.Any<Replay>()).Returns(new ReplayResponse { JobId = _job.ToString() });
        _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns([]);
        _observers = Substitute.For<IReducerObservers>();
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _reducers = new Reducers(
            _store,
            _options.ExplicitArtifacts.CreateSnapshot().ApplyTo(_artifacts),
            _provider,
            _activator,
            new ReducerValidator(),
            _store.EventTypes,
            new DefaultNamingPolicy(),
            JsonSerializerOptions.Default,
            Substitute.For<IIdentityProvider>(),
            _observers,
            new Cratis.Traces.ActivitySource<Reducers>(_trace),
            NullLogger<Reducers>.Instance);
        await _reducers.Discover();
        await _reducers.Register();
        _plain = _reducers.GetHandlerFor(typeof(PlainReducer));
        _marked = _reducers.GetHandlerFor(_markedType);
        _jobs = new JobId[2];
        var types = new[] { typeof(PlainReducer), typeof(MarkedReducer) };
        var handlers = new[] { _plain, _marked };
        for (var index = 0; index < types.Length; index++)
        {
            await _reducers.GetStateFor(types[index]);
            _jobs[index] = await _reducers.Replay(types[index]);
            await _reducers.GetFailedPartitionsFor(types[index]);
            var stream = _streams[handlers[index].Id.Value];
            var count = 0;
            foreach (var name in new[] { "delivered", "remove" })
            {
                stream.OnNext(new ReduceOperationMessage
                {
                    Partition = "source",
                    Events = [new Contracts.Events.AppendedEvent
                    {
                        Context = (EventContext.Empty with { EventType = new("event-alias", 1), EventSourceId = "source" }).ToContract(),
                        Content = JsonSerializer.Serialize(new PlainEvent(name))
                    }]
                });
                await _ledger.WaitForDisposals(types[index], ++count);
            }
            foreach (var state in new[] { ReplayState.BeginReplay, ReplayState.EndReplay, ReplayState.BeginReplayPartition, ReplayState.EndReplayPartition })
            {
                stream.OnNext(new ReduceOperationMessage { ReplayState = state, Partition = "source" });
                await _ledger.WaitForDisposals(types[index], ++count);
            }
        }
        _runtime = await _reducers.Register(typeof(RuntimeReducer));
    }

    void Destroy()
    {
        foreach (var handler in _reducers?.GetAllHandlers() ?? []) handler.Disconnect();
        foreach (var stream in _streams.Values) stream.Dispose();
        _trace.Dispose();
    }

    [Fact] void should_use_the_explicit_alias_for_type_lookup() => _plain.Id.Value.ShouldEqual("plain-alias");
    [Fact] void should_register_an_unnominated_plain_type_at_runtime() => _runtime.Id.Value.ShouldEqual(typeof(RuntimeReducer).FullName);
    [Fact] void should_keep_sequence_parity() => _plain.EventSequenceId.ShouldEqual(_marked.EventSequenceId);
    [Fact] void should_keep_tag_parity() => _definitions["plain-alias"].Tags.ShouldContainOnly(_definitions["marked-alias"].Tags);
    [Fact] void should_keep_filter_tag_parity() => _definitions["plain-alias"].Filters.FilterTags.ShouldContainOnly(_definitions["marked-alias"].Filters.FilterTags);
    [Fact] void should_keep_source_filter_parity() => _definitions["plain-alias"].Filters.EventSourceType.ShouldEqual(_definitions["marked-alias"].Filters.EventSourceType);
    [Fact] void should_keep_stream_filter_parity() => _definitions["plain-alias"].Filters.EventStreamType.ShouldEqual(_definitions["marked-alias"].Filters.EventStreamType);
    [Fact] void should_preserve_the_explicit_sequence() => _plain.EventSequenceId.Value.ShouldEqual("metadata-sequence");
    [Fact] void should_preserve_tags() => _definitions["plain-alias"].Tags.ShouldContainOnly("ops", "audit");
    [Fact] void should_preserve_filter_tags() => _definitions["plain-alias"].Filters.FilterTags.ShouldContainOnly("important");
    [Fact] void should_preserve_source_filter() => _definitions["plain-alias"].Filters.EventSourceType.ShouldEqual("orders");
    [Fact] void should_preserve_stream_filter() => _definitions["plain-alias"].Filters.EventStreamType.ShouldEqual("shipping");
    [Fact] void should_keep_plain_read_model_passivity() => _reducers.GetHandlerFor(typeof(PlainPassiveReducer)).IsActive.ShouldBeFalse();
    [Fact] void should_keep_marker_read_model_passivity() => _reducers.GetHandlerFor<MarkedPassiveReducer>().IsActive.ShouldBeFalse();
    [Fact] void should_not_subscribe_passive_reducers_during_registration() => (_definitions.ContainsKey("plain-passive") || _definitions.ContainsKey("marked-passive")).ShouldBeFalse();
    [Fact] void should_return_native_replay_jobs() => _jobs.ShouldContainOnly(new JobId(_job), new JobId(_job));
    [Fact] void should_route_type_state_operations_to_aliases() => _services.Observers.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "GetObserverInformation").Select(call => ((GetObserverInformationRequest)call.GetArguments()[0]!).ObserverId).ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_route_type_replay_operations_to_aliases() => _services.Observers.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Replay").Select(call => ((Replay)call.GetArguments()[0]!).ObserverId).ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_route_failed_partition_operations_to_aliases() => _services.FailedPartitions.ReceivedCalls().Select(call => ((GetFailedPartitionsRequest)call.GetArguments()[0]!).ObserverId).ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_notify_plain_model_observers_after_reduction() => _observers.Received(1).NotifyChange(_store.Namespace, new ReadModelKey("source"), new PlainModel("delivered"), false);
    [Fact] void should_notify_marker_model_observers_after_reduction() => _observers.Received(1).NotifyChange(_store.Namespace, new ReadModelKey("source"), new MarkedModel("delivered"), false);
    [Fact] void should_notify_plain_model_removal() => _observers.Received(1).NotifyChange<PlainModel>(_store.Namespace, new ReadModelKey("source"), null, true);
    [Fact] void should_notify_marker_model_removal() => _observers.Received(1).NotifyChange<MarkedModel>(_store.Namespace, new ReadModelKey("source"), null, true);
    [Fact] void should_match_marker_activation_invocation_notifications_and_disposal() => _ledger.ActionsFor(typeof(PlainReducer)).ShouldEqual(_ledger.ActionsFor(typeof(MarkedReducer)));
    [Fact] void should_activate_and_dispose_per_delivery_and_notification() => _ledger.ActionsFor(typeof(PlainReducer)).Count(_ => _ == "disposed").ShouldEqual(6);
    [Fact] void should_deliver_every_replay_notification() => _ledger.ActionsFor(typeof(PlainReducer)).Where(_ => _ is not "created" and not "disposed").ShouldContainOnly("event", "event", "begin", "end", "begin:source", "end:source");

    public record PlainModel(string Name);
    public record MarkedModel(string Name);
    [Passive] public record PlainPassiveModel(string Name);
    [Passive] public record MarkedPassiveModel(string Name);
    public record RuntimeModel(string Name);

    [EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping")]
    public class PlainReducer(ObserverLifecycle.Ledger ledger) : ObserverLifecycle(ledger)
    {
        public PlainModel? On(PlainEvent @event, PlainModel? current)
        {
            Record("event");
            return @event.Name == "remove" ? null : new(@event.Name);
        }
    }
    [Reducer("marked-alias"), EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping")]
    public class MarkedReducer(ObserverLifecycle.Ledger ledger) : ObserverLifecycle(ledger), IReducerFor<MarkedModel>
    {
        public MarkedModel? On(PlainEvent @event, MarkedModel? current)
        {
            Record("event");
            return @event.Name == "remove" ? null : new(@event.Name);
        }
    }
    public class PlainPassiveReducer
    {
        public PlainPassiveModel On(PlainEvent @event, PlainPassiveModel? current) => new(@event.Name);
    }
    [Reducer("marked-passive")]
    public class MarkedPassiveReducer : IReducerFor<MarkedPassiveModel>
    {
        public MarkedPassiveModel On(PlainEvent @event, MarkedPassiveModel? current) => new(@event.Name);
    }
    public class RuntimeReducer
    {
        public RuntimeModel On(PlainEvent @event, RuntimeModel? current) => new(@event.Name);
    }
}
