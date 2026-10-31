// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Registrations.for_PlainObserverParity.given;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_a_plain_reactor;

public class and_operations_metadata_and_lifetimes_match_markers : an_explicit_store
{
    readonly Dictionary<string, Subject<EventsToObserve>> _streams = [];
    readonly Dictionary<string, ReactorDefinition> _definitions = [];
    readonly Type _markedType = typeof(MarkedObserver);
    ObserverLifecycle.Ledger _ledger;
    System.Diagnostics.ActivitySource _trace;
    IReactorHandler _plain;
    IReactorHandler _marked;
    IReactorHandler _runtime;
    CancellationToken _runtimeToken;
    JobId[] _jobs;
    Observation.ReactorPartitionRetryOutcome[] _retryOutcomes;
    Observation.ReactorPartitionQuarantineClearResult[] _clearOutcomes;
    Guid _job;

    void Establish()
    {
        _provider.Dispose();
        _ledger = new();
        _trace = new("reactor-parity");
        var services = new ServiceCollection().AddSingleton(_ledger);
        services.AddKeyedSingleton<IActivitySource<EventSequence>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<EventSequence>>());
        services.AddKeyedSingleton<IActivitySource<Reactors>>(ClientActivity.SourceName, (_, _) => new Cratis.Traces.ActivitySource<Reactors>(_trace));
        services.AddKeyedSingleton<IActivitySource<Reducers.Reducers>>(ClientActivity.SourceName, (_, _) => Substitute.For<IActivitySource<Reducers.Reducers>>());
        _provider = services.BuildServiceProvider();
        _activator = new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance);
        _artifacts.Reactors.Returns([typeof(MarkedObserver)]);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>(_ => _.WithId("event-alias")).RegisterReactor<PlainObserver>("plain-alias");
        _services.Reactors.Observe(Arg.Any<IObservable<ReactorMessage>>(), Arg.Any<CallContext>()).Returns(call =>
        {
            string id = null!;
            call.Arg<IObservable<ReactorMessage>>().Take(1).Subscribe(message =>
            {
                var definition = message.Content.Value0!.Reactor;
                id = definition.ReactorId;
                _definitions[id] = definition;
                _streams[id] = new();
            });
            return _streams[id];
        });
        _job = Guid.NewGuid();
        _services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>()).Returns(new ObserverInformation());
        _services.Observers.Replay(Arg.Any<Replay>()).Returns(new ReplayResponse { JobId = _job.ToString() });
        _services.Observers.RetryPartition(Arg.Any<RetryPartition>()).Returns(new RetryPartitionResponse { Outcome = PartitionRecoveryOutcome.Started });
        _services.Observers.ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantine>()).Returns(new ClearPartitionQuarantineResponse
        {
            Outcome = ClearPartitionQuarantineOutcome.Cleared, RetryOutcome = PartitionRecoveryOutcome.Started
        });
        _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns([]);
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        await _store.Reactors.Register();
        _plain = _store.Reactors.GetHandlerFor(typeof(PlainObserver));
        _marked = _store.Reactors.GetHandlerFor(_markedType);
        _jobs = new JobId[2];
        _retryOutcomes = new Observation.ReactorPartitionRetryOutcome[2];
        _clearOutcomes = new Observation.ReactorPartitionQuarantineClearResult[2];
        var types = new[] { typeof(PlainObserver), typeof(MarkedObserver) };
        var handlers = new[] { _plain, _marked };
        for (var index = 0; index < types.Length; index++)
        {
            await _store.Reactors.GetStateFor(types[index]);
            _jobs[index] = await _store.Reactors.Replay(types[index]);
            await _store.Reactors.GetFailedPartitionsFor(types[index]);
            var recovery = (IReactorPartitionRecovery)_store.Reactors;
            _retryOutcomes[index] = await recovery.RetryFailedPartitionFor(types[index], "partition");
            _clearOutcomes[index] = await recovery.ClearFailedPartitionQuarantineFor(types[index], "partition", retryImmediately: false);
            var stream = _streams[handlers[index].Id.Value];
            stream.OnNext(new EventsToObserve
            {
                Partition = "partition",
                Events = [new Contracts.Events.AppendedEvent
                {
                    Context = (EventContext.Empty with { EventType = new("event-alias", 1) }).ToContract(), Content = "{\"Name\":\"delivered\"}"
                }]
            });
            await _ledger.WaitForDisposals(types[index], 1);
            var count = 1;
            foreach (var state in new[] { ReplayState.BeginReplay, ReplayState.EndReplay, ReplayState.BeginReplayPartition, ReplayState.EndReplayPartition })
            {
                stream.OnNext(new EventsToObserve { ReplayState = state, Partition = "partition" });
                await _ledger.WaitForDisposals(types[index], ++count);
            }
        }
        _runtime = await _store.Reactors.Register(typeof(RuntimeObserver));
        _runtimeToken = _runtime.CancellationToken;
        _store.Reactors.Unregister(_runtime.Id);
    }

    void Destroy()
    {
        _plain?.Disconnect();
        _marked?.Disconnect();
        foreach (var stream in _streams.Values) stream.Dispose();
        _trace.Dispose();
    }

    [Fact] void should_resolve_the_explicit_alias_for_type_lookup() => _plain.Id.Value.ShouldEqual("plain-alias");
    [Fact] void should_register_an_unnominated_plain_type_at_runtime() => _runtime.Id.Value.ShouldEqual(typeof(RuntimeObserver).FullName);
    [Fact] void should_cancel_the_runtime_handlers_stream_on_unregister() => _runtimeToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_keep_sequence_parity() => _definitions["plain-alias"].EventSequenceId.ShouldEqual(_definitions["marked-alias"].EventSequenceId);
    [Fact] void should_keep_tag_parity() => _definitions["plain-alias"].Tags.ShouldContainOnly(_definitions["marked-alias"].Tags);
    [Fact] void should_keep_filter_tag_parity() => _definitions["plain-alias"].Filters.FilterTags.ShouldContainOnly(_definitions["marked-alias"].Filters.FilterTags);
    [Fact] void should_keep_source_filter_parity() => _definitions["plain-alias"].Filters.EventSourceType.ShouldEqual(_definitions["marked-alias"].Filters.EventSourceType);
    [Fact] void should_keep_stream_filter_parity() => _definitions["plain-alias"].Filters.EventStreamType.ShouldEqual(_definitions["marked-alias"].Filters.EventStreamType);
    [Fact] void should_preserve_the_explicit_sequence() => _definitions["plain-alias"].EventSequenceId.ShouldEqual("metadata-sequence");
    [Fact] void should_preserve_tags() => _definitions["plain-alias"].Tags.ShouldContainOnly("ops", "audit");
    [Fact] void should_preserve_filter_tags() => _definitions["plain-alias"].Filters.FilterTags.ShouldContainOnly("important");
    [Fact] void should_preserve_source_filter() => _definitions["plain-alias"].Filters.EventSourceType.ShouldEqual("orders");
    [Fact] void should_preserve_stream_filter() => _definitions["plain-alias"].Filters.EventStreamType.ShouldEqual("shipping");
    [Fact] void should_keep_replayability_parity() => (_definitions["plain-alias"].IsNotReplayable && _definitions["marked-alias"].IsNotReplayable).ShouldBeTrue();
    [Fact] void should_return_the_native_replay_jobs() => _jobs.ShouldContainOnly(new JobId(_job), new JobId(_job));
    [Fact] void should_route_state_operations_to_aliases() => StateIds().ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_route_failed_partition_operations_to_aliases() => _services.FailedPartitions.ReceivedCalls().Select(call => ((GetFailedPartitionsRequest)call.GetArguments()[0]!).ObserverId).ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_route_replay_operations_to_aliases() => _services.Observers.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "Replay").Select(call => ((Replay)call.GetArguments()[0]!).ObserverId).ShouldContainOnly("plain-alias", "marked-alias");
    [Fact] void should_return_the_same_retry_outcome_for_plain_and_marker_types() => _retryOutcomes.All(_ => _ == Observation.ReactorPartitionRetryOutcome.Started).ShouldBeTrue();
    [Fact] void should_return_the_same_clear_and_retry_outcomes_for_plain_and_marker_types() => _clearOutcomes.All(_ => _.Outcome == Observation.ReactorPartitionQuarantineClearOutcome.Cleared && _.RetryOutcome == Observation.ReactorPartitionRetryOutcome.Started).ShouldBeTrue();
    [Fact] void should_route_retry_with_the_alias_sequence_and_partition() => _services.Observers.Received(1).RetryPartition(Arg.Is<RetryPartition>(request => request.ObserverId == "plain-alias" && request.EventSequenceId == "metadata-sequence" && request.Partition == "partition"));
    [Fact] void should_route_clear_with_the_alias_sequence_and_retry_choice() => _services.Observers.Received(1).ClearPartitionQuarantine(Arg.Is<ClearPartitionQuarantine>(request => request.ObserverId == "plain-alias" && request.EventSequenceId == "metadata-sequence" && request.Partition == "partition" && !request.RetryImmediately));
    [Fact] void should_match_marker_activation_invocation_notifications_and_disposal() => _ledger.ActionsFor(typeof(PlainObserver)).ShouldEqual(_ledger.ActionsFor(typeof(MarkedObserver)));
    [Fact] void should_activate_and_dispose_once_per_delivery_and_notification() => _ledger.ActionsFor(typeof(PlainObserver)).Count(_ => _ == "disposed").ShouldEqual(5);
    [Fact] void should_deliver_every_replay_notification() => _ledger.ActionsFor(typeof(PlainObserver)).Where(_ => _ is not "created" and not "disposed").ShouldContainOnly("event", "begin", "end", "begin:partition", "end:partition");

    IEnumerable<string> StateIds() => _services.Observers.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "GetObserverInformation").Select(call => ((GetObserverInformationRequest)call.GetArguments()[0]!).ObserverId);

    [EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping"), OnceOnly]
    public class PlainObserver(ObserverLifecycle.Ledger ledger) : ObserverLifecycle(ledger)
    {
        public void On(PlainEvent @event) => Record("event");
    }

    [Reactor("marked-alias"), EventSequence("metadata-sequence"), Tags("ops", "audit"), FilterEventsByTag("important"), EventSourceType("orders"), EventStreamType("shipping"), OnceOnly]
    public class MarkedObserver(ObserverLifecycle.Ledger ledger) : ObserverLifecycle(ledger), IReactor
    {
        public void On(PlainEvent @event) => Record("event");
    }

    public class RuntimeObserver
    {
        public void On(PlainEvent @event) { }
    }
}
