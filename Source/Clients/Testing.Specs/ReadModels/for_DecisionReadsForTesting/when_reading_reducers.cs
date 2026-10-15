// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_reading_reducers : Specification
{
    static readonly AsyncLocal<Action?> _callback = new();

    static ChronicleOptions Options(Type reducer)
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Credit>().RegisterReducer(reducer);
        return options;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_use_full_source_replay_for_instance_and_static_reducers(bool self)
    {
        using var store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider(), Options(self ? typeof(StaticBalance) : typeof(BalanceReducer)));
        var source = EventSourceId.New();
        await store.EventLog.Append(source, new Credit(5));
        await store.EventLog.Append(EventSourceId.New(), new Credit(99));
        await store.EventLog.Append(source, new Credit(-2));
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var value = self
            ? (await store.GetDecisionReads().GetFor<StaticBalance>(unit, source)).Instance!.Amount
            : (await store.GetDecisionReads().GetFor<Balance>(unit, source)).Instance!.Amount;
        value.ShouldEqual(3);
        unit.HasEnrolledDecisionReads.ShouldBeTrue();
        unit.AddEvent(EventSequenceId.Log, source, new Credit(1), Causation.Unknown());
        var concrete = (UnitOfWork)unit;
        await concrete.CommitAsOwner(concrete.ClaimDecisionReadCommitOwnership());
        unit.IsSuccess.ShouldBeTrue();
        var guard = unit.GetAppendConditions().Single();
        guard.EventSourceId.ShouldEqual(source);
        guard.EventTypes.Single().Id.ShouldEqual(store.EventTypes.GetEventTypeFor(typeof(Credit)).Id);
        guard.EventSourceType.ShouldBeNull();
        guard.EventStreamType.ShouldBeNull();
        guard.EventStreamId.ShouldBeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_guard_absence_or_a_null_result(bool removed)
    {
        using var store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider(), Options(typeof(BalanceReducer)));
        var source = EventSourceId.New();
        if (removed)
        {
            await store.EventLog.Append(source, new Credit(5));
            await store.EventLog.Append(source, new Credit(-100));
        }
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        var read = await store.GetDecisionReads().GetFor<Balance>(unit, source);
        read.Instance.ShouldBeNull();
        unit.HasEnrolledDecisionReads.ShouldBeTrue();
        read.IsProtected.ShouldBeTrue();
        read.Key.Value.ShouldEqual(source.Value);
    }

    [Theory]
    [InlineData(ReadAllowance.Advisory, false)]
    [InlineData(ReadAllowance.Contributed, false)]
    [InlineData(ReadAllowance.Advisory, true)]
    [InlineData(ReadAllowance.Contributed, true)]
    public async Task should_refuse_real_chronicle_reads_in_callbacks_despite_inherited_allowances(ReadAllowance allowance, bool self)
    {
        using var store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider(), Options(self ? typeof(StaticBalance) : typeof(BalanceReducer)));
        var source = EventSourceId.New();
        await store.EventLog.Append(source, new Credit(5));
        var unit = store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        using var outer = unit.AllowReads(allowance, "outer");
        var errors = new List<Exception>();
        _callback.Value = () => errors.Add(Catch.Exception(() => store.EventLog.GetTailSequenceNumber().GetAwaiter().GetResult()));
        try
        {
            if (self) await store.GetDecisionReads().GetFor<StaticBalance>(unit, source);
            else await store.GetDecisionReads().GetFor<Balance>(unit, source);
            errors.Count.ShouldEqual(self ? 1 : 2);
            foreach (var error in errors) error.ShouldBeOfExactType<ReadRefusedWhileReadsAreProtected>();

            // The transport allowance is not leaked, and the caller's outer allowance is restored.
            await store.EventLog.GetTailSequenceNumber();
        }
        finally
        {
            _callback.Value = null;
        }
    }

    [Theory]
    [InlineData(typeof(DependentReducer), DecisionReadRefusalReason.ReducerDependencies)]
    [InlineData(typeof(FilteredReducer), DecisionReadRefusalReason.ReducerFilters)]
    public async Task should_share_fail_closed_admission_with_the_client(Type reducer, DecisionReadRefusalReason reason)
    {
        using var store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider(), Options(reducer));
        var error = reducer == typeof(DependentReducer)
            ? await Record.ExceptionAsync(() => store.GetDecisionReads().GetDetached<DependentBalance>("source"))
            : await Record.ExceptionAsync(() => store.GetDecisionReads().GetDetached<FilteredBalance>("source"));
        ((DecisionReadRefused)error).Reason.ShouldEqual(reason);
    }

    [Fact]
    public async Task should_match_the_scenario_fold_when_replaying_the_same_seeded_events()
    {
        var defaults = new Defaults(new InMemoryClientArtifactsProvider(), null, Options(typeof(BalanceReducer)));
        using var store = (EventStoreForTesting)defaults.EventStore;
        using var scenario = new ReadModelScenario<Balance>(null, defaults);
        var source = EventSourceId.New();
        Credit[] events = [new(6), new(-2)];
        await scenario.Given.ForEventSource(source).Events(events);
        scenario.Instance!.Amount.ShouldEqual(4);
        foreach (var input in events) await store.EventLog.Append(source, input);
        (await store.GetDecisionReads().GetDetached<Balance>(source)).Instance.ShouldEqual(scenario.Instance);
    }

    public record Credit(int Amount);
    public record Balance(int Amount);
    public class BalanceReducer : IReducerFor<Balance>
    {
        public BalanceReducer() => _callback.Value?.Invoke();
        public Balance? Reduce(Credit input, Balance? current)
        {
            _callback.Value?.Invoke();
            return input.Amount == -100 ? null : new((current?.Amount ?? 0) + input.Amount);
        }
    }
    public record StaticBalance(int Amount)
    {
        public static StaticBalance Reduce(Credit input, StaticBalance? current)
        {
            _callback.Value?.Invoke();
            return new((current?.Amount ?? 0) + input.Amount);
        }
    }
    public record DependentBalance(int Amount);
    public class DependentReducer(IEventStore store) : IReducerFor<DependentBalance>
    {
        public IEventStore Store { get; } = store;
        public DependentBalance Reduce(Credit input, DependentBalance? current) => new((current?.Amount ?? 0) + input.Amount);
    }
    public record FilteredBalance(int Amount);
    [FilterEventsByTag("accepted")]
    public class FilteredReducer : IReducerFor<FilteredBalance>
    {
        public FilteredBalance Reduce(Credit input, FilteredBalance? current) => new((current?.Amount ?? 0) + input.Amount);
    }
}
