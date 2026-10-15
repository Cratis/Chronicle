// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Transactions;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;
using EventContext = Cratis.Chronicle.Events.EventContext;
using EventType = Cratis.Chronicle.Events.EventType;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_reading_reducers : given.a_decision_reader
{
    static readonly AsyncLocal<Action?> _callback = new();
    readonly List<(UnitOfWork Unit, DecisionReadCommitOwner Owner)> _units = [];
    ServiceProvider _provider;
    Reducers.Reducers _native;
    IEventTypes _types;
    IReducerHandler _handler;
    List<AppendedEventResponse> _events;
    int _fetches;

    void Establish()
    {
        _provider = new ServiceCollection().AddLogging().BuildServiceProvider();
        _types = Substitute.For<IEventTypes>();
        _types.AllClrTypes.Returns(new[] { typeof(Added), typeof(AddedV2), typeof(Removed) }.ToImmutableList());
        _types.HasFor(Arg.Any<EventTypeId>()).Returns(true);
        _types.GetEventTypeFor(typeof(Added)).Returns(new EventType("added", 1));
        _types.GetEventTypeFor(typeof(AddedV2)).Returns(new EventType("added", 2));
        _types.GetEventTypeFor(typeof(Removed)).Returns(new EventType("removed", 1));
        _types.GetClrTypeFor((EventTypeId)"added", (EventTypeGeneration)1).Returns(typeof(Added));
        _types.GetClrTypeFor((EventTypeId)"added", (EventTypeGeneration)2).Returns(typeof(AddedV2));
        _types.GetClrTypeFor((EventTypeId)"removed", (EventTypeGeneration)1).Returns(typeof(Removed));
        _store.EventTypes.Returns(_types);
        _native = new(
            _store,
            Substitute.For<IClientArtifactsProvider>(),
            _provider,
            new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance),
            Substitute.For<IReducerValidator>(),
            _types,
            new Cratis.Serialization.DefaultNamingPolicy(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            new BaseIdentityProvider(),
            new ReducerObservers(),
            Substitute.For<IActivitySource<Reducers.Reducers>>(),
            NullLogger<Reducers.Reducers>.Instance);
        _store.Reducers.Returns(_native);
        Configure(typeof(CountingReducer));
        _events = [Event(1, 3), Event(4, -1)];
        _sequences.ForEventSourceIdAndEventTypes(Arg.Any<ForEventSourceIdAndEventTypesRequest>(), Arg.Any<CallContext>())
            .Returns(_ =>
            {
                ++_fetches;
                return QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.NewGuid(), _events.ToArray());
            });
    }

    async Task Destroy()
    {
        _callback.Value = null;
        foreach (var (unit, owner) in _units)
        {
            await unit.RollbackAsOwner(owner);
            unit.Dispose();
        }
        await _provider.DisposeAsync();
    }

    UnitOfWork CreateUnit(IEventStore store)
    {
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, store);
        _units.Add((unit, unit.ClaimDecisionReadCommitOwnership()));
        return unit;
    }

    void Configure(Type reducer, bool self = false)
    {
        var invoker = self
            ? (IReducerInvoker)new SelfReducerInvoker(_types, typeof(SelfModel), "models")
            : new ReducerInvoker(_types, new ClientArtifactsActivator(_provider, NullLoggerFactory.Instance), reducer, typeof(Counter), "models");
        _handler = Substitute.For<IReducerHandler>();
        _handler.ReducerType.Returns(reducer);
        _handler.EventSequenceId.Returns(EventSequenceId.Log);
        _handler.EventTypes.Returns(invoker.EventTypes);
        _handler.Invoker.Returns(invoker);
        typeof(Reducers.Reducers).GetField("_handlersByModelType", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_native, new Dictionary<Type, IReducerHandler> { [self ? typeof(SelfModel) : typeof(Counter)] = _handler });
    }

    static AppendedEventResponse Event(ulong position, int amount, uint generation = 1) => new()
    {
        Context = new() { EventSourceId = "source", EventType = new() { Id = "added", Generation = generation }, SequenceNumber = position },
        Content = JsonSerializer.Serialize(new { Amount = amount })
    };

    [Fact]
    public async Task should_replay_all_history_from_null_not_the_sink_and_enroll_before_return()
    {
        var unit = CreateUnit(_store);
        using var frame = unit.EnforceReadProtection();
        var read = await _reader.GetFor<Counter>(unit, "source");
        read.Instance!.Value.ShouldEqual(2);
        unit.HasEnrolledDecisionReads.ShouldBeTrue();
        var condition = ((IDecisionRead)read).Scope;
        condition.EventTypes.Select(type => type.Id.Value).Order().ShouldEqual(["added", "removed"]);
        condition.EventSourceId.ShouldEqual((EventSourceId)"source");
        condition.EventSourceType.ShouldBeNull();
        condition.EventStreamType.ShouldBeNull();
        condition.EventStreamId.ShouldBeNull();
        await _readModels.DidNotReceiveWithAnyArgs().GetInstanceByKey(default!, default);
        await _sequences.Received(1).ForEventSourceIdAndEventTypes(
            Arg.Is<ForEventSourceIdAndEventTypesRequest>(request =>
                request.EventStore == "store" && request.Namespace == "namespace" && request.EventSequenceId == "event-log" &&
                request.EventSourceId == "source" && string.IsNullOrEmpty(request.EventStreamType) && string.IsNullOrEmpty(request.EventStreamId)),
            Arg.Any<CallContext>());
    }

    [Fact]
    public async Task should_select_the_declared_generation_even_when_the_registry_also_has_v2()
    {
        _events[0] = Event(1, 999, 2);
        _events[0].GenerationalContent = new Dictionary<int, string> { [1] = "{\"amount\":3}" };
        (await _reader.GetDetached<Counter>("source")).Instance!.Value.ShouldEqual(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_unavailable_or_revised_generations(bool revised)
    {
        _events[0] = Event(1, 999, 2);
        if (revised)
        {
            _events[0].GenerationalContent = new Dictionary<int, string> { [1] = "{\"amount\":3}" };
            _events[0].Revisions = [new() { Generation = 2, Content = "{\"amount\":999}" }];
        }
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerGeneration);
    }

    [Fact]
    public async Task should_refuse_a_revision_marker_without_revision_history()
    {
        // Storage that revises in place (SQL) sends only the revised generation, with no revision list.
        _events[0] = Event(1, 999, 2);
        _events[0].GenerationalContent = new Dictionary<int, string> { [1] = "{\"amount\":3}", [2] = "{\"amount\":999}" };
        _events[0].RevisedGeneration = 2;
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerGeneration);
    }

    [Fact]
    public async Task should_use_in_place_revised_content_of_the_declared_generation()
    {
        _events[0] = Event(1, 999, 2);
        _events[0].GenerationalContent = new Dictionary<int, string> { [1] = "{\"amount\":3}" };
        _events[0].RevisedGeneration = 1;
        (await _reader.GetDetached<Counter>("source")).Instance!.Value.ShouldEqual(2);
    }

    [Fact]
    public async Task should_refuse_registry_fallback_to_a_different_generation()
    {
        _types.GetClrTypeFor((EventTypeId)"added", (EventTypeGeneration)1).Returns(typeof(AddedV2));
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerGeneration);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_require_dispatch_evidence_even_when_the_tail_probe_passes(bool self)
    {
        Configure(self ? typeof(SelfModel) : typeof(CountingReducer), self);

        // Both real invokers must expose a skipped middle event even though they reach the tail.
        var incomplete = await _handler.Invoker.Invoke(
            _provider,
            [new(new AddedV2(999), EventContext.Empty with { SequenceNumber = 1 }), new(new Added(-1), EventContext.Empty with { SequenceNumber = 4 })],
            null);
        incomplete.DispatchedEvents.ShouldEqual(1);
        incomplete.LastSuccessfullyObservedEvent.ShouldEqual((EventSequenceNumber)4);
        var staleInvoker = Substitute.For<IReducerInvoker>();
        staleInvoker.Invoke(Arg.Any<IServiceProvider>(), Arg.Any<IEnumerable<EventAndContext>>(), Arg.Is<object?>(value => value == null)).Returns(incomplete);
        _handler.Invoker.Returns(staleInvoker);
        var error = self
            ? await Record.ExceptionAsync(() => _reader.GetDetached<SelfModel>("source"))
            : await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerDispatchIncomplete);
        _fetches.ShouldEqual(1);
    }

    [Fact]
    public async Task should_retry_a_fold_below_the_probe_then_refuse()
    {
        _probe = 6;
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.FoldIncomplete);
        _fetches.ShouldEqual(3);
    }

    [Fact]
    public async Task should_replay_again_when_a_fold_passes_the_boundary()
    {
        _boundary = 2;
        var read = await _reader.GetDetached<Counter>("source");
        read.Instance!.Value.ShouldEqual(2);
        _fetches.ShouldEqual(3);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_guard_absence_and_null_removal(bool removed)
    {
        if (removed)
        {
            _events.Add(new() { Context = new() { EventSourceId = "source", EventType = new() { Id = "removed", Generation = 1 }, SequenceNumber = 5 }, Content = "{}" });
            _probe = 5;
        }
        else
        {
            _events.Clear();
            _probe = EventSequenceNumber.Unavailable.Value;
        }
        var unit = CreateUnit(_store);
        using var frame = unit.EnforceReadProtection();
        var read = await _reader.GetFor<Counter>(unit, "source");
        read.Instance.ShouldBeNull();
        unit.HasEnrolledDecisionReads.ShouldBeTrue();
        read.IsProtected.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ReadAllowance.Advisory, false)]
    [InlineData(ReadAllowance.Contributed, false)]
    [InlineData(ReadAllowance.Advisory, true)]
    [InlineData(ReadAllowance.Contributed, true)]
    public async Task should_suspend_outer_allowances_for_constructors_and_static_or_instance_handlers(ReadAllowance allowance, bool self)
    {
        Configure(self ? typeof(SelfModel) : typeof(CountingReducer), self);
        var unit = CreateUnit(_store);
        using var frame = unit.EnforceReadProtection();
        using var outer = unit.AllowReads(allowance, "outer");
        var callbacks = 0;
        _callback.Value = () =>
        {
            ++callbacks;
            ReadProtectionReadRegion.IsAllowed.ShouldBeFalse();
            Catch.Exception(() => ReadProtectionGuard.Read("handler read")).ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();
            Catch.Exception(() => ReadProtectionGuard.Append("handler append")).ShouldBeOfExactType<ImmediateAppendWhileReadsAreProtected>();
        };
        if (self) await _reader.GetFor<SelfModel>(unit, "source");
        else await _reader.GetFor<Counter>(unit, "source");
        callbacks.ShouldEqual(self ? 2 : 3);
        ReadProtectionReadRegion.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public async Task should_restore_outer_allowances_after_a_callback_failure_without_enrollment()
    {
        var unit = CreateUnit(_store);
        using var frame = unit.EnforceReadProtection();
        using var outer = unit.AllowReads(ReadAllowance.Advisory, "outer");
        _callback.Value = () => ReadProtectionGuard.Read("constructor read");
        (await Record.ExceptionAsync(() => _reader.GetFor<Counter>(unit, "source"))).ShouldBeOfExactType<InvalidOperationException>();
        ReadProtectionReadRegion.IsAllowed.ShouldBeTrue();
        unit.HasEnrolledDecisionReads.ShouldBeFalse();
        unit.GetAppendConditions().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_admit_static_models_without_activating_their_constructor()
    {
        Configure(typeof(SelfModel), true);
        (await _reader.GetDetached<SelfModel>("source")).Instance!.Value.ShouldEqual(2);
    }

    [Theory]
    [InlineData(typeof(CountingReducer), true)]
    [InlineData(typeof(LoggingReducer), true)]
    [InlineData(typeof(ServiceReducer), false)]
    [InlineData(typeof(OptionalServiceReducer), false)]
    public void should_admit_only_infrastructure_constructor_dependencies(Type reducer, bool admitted)
    {
        Configure(reducer);
        _reader.Admit<Counter>().IsAdmitted.ShouldEqual(admitted);
        if (!admitted) _reader.Admit<Counter>().Reason.ShouldEqual(DecisionReadRefusalReason.ReducerDependencies);
    }

    [Theory]
    [InlineData("inbox", false, DecisionReadRefusalReason.NotEventLog)]
    [InlineData("event-log", true, DecisionReadRefusalReason.NoEventTypes)]
    public async Task should_refuse_unsafe_handler_shapes_before_transport(string sequence, bool noTypes, DecisionReadRefusalReason reason)
    {
        _handler.EventSequenceId.Returns((EventSequenceId)sequence);
        if (noTypes) _handler.EventTypes.Returns([]);
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(reason);
        _fetches.ShouldEqual(0);
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("*")]
    [InlineData("source#alias")]
    [InlineData(" source")]
    public async Task should_refuse_non_source_keys_before_transport(string key)
    {
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>(key));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.InvalidKey);
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(typeof(TagFilteredReducer))]
    [InlineData(typeof(SourceFilteredReducer))]
    [InlineData(typeof(StreamFilteredReducer))]
    public async Task should_refuse_filtered_reducers_without_activating_or_reading_them(Type reducer)
    {
        Configure(reducer);
        _callback.Value = () => throw new InvalidOperationException("must not activate");
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerFilters);
        error.Message.ShouldContain("reducer filters its events");
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_a_constructor_service_dependency_before_activating_or_reading()
    {
        Configure(typeof(ServiceReducer));
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.ReducerDependencies);
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_refuse_unsupported_event_type_encoding()
    {
        _handler.EventTypes.Returns([new EventType("a,b", 1)]);
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.UnsupportedEventTypeId);
        _sequences.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task should_enforce_staged_overlap_before_exposing_the_reducer_value()
    {
        var unit = CreateUnit(_store);
        using var frame = unit.EnforceReadProtection();
        unit.AddEvent(EventSequenceId.Log, "source", new Added(5), Cratis.Chronicle.Auditing.Causation.Unknown());
        var error = await Record.ExceptionAsync(() => _reader.GetFor<Counter>(unit, "source"));
        error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
        unit.HasEnrolledDecisionReads.ShouldBeFalse();
        unit.GetAppendConditions().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_cross_store_or_namespace_enrollment(bool otherNamespace)
    {
        var other = Substitute.For<IEventStore>();
        other.Name.Returns((EventStoreName)(otherNamespace ? "store" : "other"));
        other.Namespace.Returns((EventStoreNamespaceName)(otherNamespace ? "other" : "namespace"));
        var unit = CreateUnit(other);
        using var frame = unit.EnforceReadProtection();
        var error = await Record.ExceptionAsync(() => _reader.GetFor<Counter>(unit, "source"));
        error.ShouldBeOfExactType<ReadDependencyTargetMismatch>();
        unit.HasEnrolledDecisionReads.ShouldBeFalse();
        unit.GetAppendConditions().ShouldBeEmpty();
    }

    [Fact]
    public void should_describe_the_guard_types_without_projection_pinned_delivery()
    {
        _reader.Describe<Counter>().Admission.IsAdmitted.ShouldBeTrue();
        _reader.Describe<Counter>().UsesPinnedGenerationDelivery.ShouldBeFalse();
    }

    [Fact]
    public async Task should_refuse_stream_narrowing_instead_of_changing_the_reducer_value()
    {
        var error = await Record.ExceptionAsync(() => _reader.GetDetached<Counter>("source", "stream", "one"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(DecisionReadRefusalReason.StreamScopeNotSupported);
        _fetches.ShouldEqual(0);
    }

    public record Added(int Amount);
    public record AddedV2(int Amount);
    public record Removed;
    public record Counter(int Value);
    public class CountingReducer : IReducerFor<Counter>
    {
        public CountingReducer() => _callback.Value?.Invoke();
        public Counter Add(Added input, Counter? current)
        {
            _callback.Value?.Invoke();
            return new((current?.Value ?? 0) + input.Amount);
        }
        public Counter? Remove(Removed input, Counter? current) => null;
    }
    [FilterEventsByTag("lending")]
    public class TagFilteredReducer : CountingReducer;
    [EventSourceType("books")]
    public class SourceFilteredReducer : CountingReducer;
    [EventStreamType("loans")]
    public class StreamFilteredReducer : CountingReducer;
    public class LoggingReducer(ILogger<LoggingReducer> logger) : CountingReducer
    {
        public ILogger<LoggingReducer> Logger { get; } = logger;
    }
    public class ServiceReducer(IEventStore store) : CountingReducer
    {
        public IEventStore Store { get; } = store;
    }
    public class OptionalServiceReducer(IEventStore? store = null) : CountingReducer
    {
        public IEventStore? Store { get; } = store;
    }
    public record SelfModel(int Value)
    {
        public static SelfModel Reduce(Added input, SelfModel? current)
        {
            _callback.Value?.Invoke();
            return new((current?.Value ?? 0) + input.Amount);
        }
    }
}
