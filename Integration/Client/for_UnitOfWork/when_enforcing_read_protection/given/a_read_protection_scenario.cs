// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

public class a_read_protection_scenario(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId Source = EventSourceId.New();
    public UnitOfWork Unit;
    public DecisionReadCommitOwner Owner;
    public DecisionRead<StockState> Read;
    public Exception Error;

    public override IEnumerable<Type> EventTypes => [typeof(StockSet), typeof(StockSetV2), typeof(StockRemoved), typeof(RoutingFact)];
    public override IEnumerable<Type> EventTypeMigrators => [typeof(StockSetToV2Migrator)];
    public override IEnumerable<Type> ModelBoundProjections => [typeof(StockState)];

    public void CreateUnit()
    {
        Unit = new(CorrelationId.New(), _ => { }, EventStore);
        Owner = Unit.ClaimDecisionReadCommitOwnership();
    }

    public async Task Acquire() => Read = await EventStore.GetDecisionReads().GetFor<StockState>(Unit, Source);

    public void Stage(ConcurrencyScope? scope = null, EventEnrollmentOptions? options = null) =>
        Unit.AddEventWithOptions(EventSequenceId.Log, Source, new StockSet(2), Causation.Unknown(), options ?? new() { ConcurrencyScope = scope });

    public Task Commit() => Unit.CommitAsOwner(Owner);

    public async Task Independent(Func<Task> append)
    {
        Task competitor;
        using (ExecutionContext.SuppressFlow())
        {
            competitor = Task.Run(append);
        }
        await competitor;
    }

    /// <summary>Waits until the system event sequences reactor has applied every requested history mutation.</summary>
    /// <returns>An awaitable task.</returns>
    /// <remarks>Runs outside the unit's read-protection frame, so its diagnostic reads are not enrolled in the decision.</remarks>
    public Task WaitForHistoryMutation() => Independent(WaitForHistoryMutationOutsideTheUnit);

    async Task WaitForHistoryMutationOutsideTheUnit()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var systemLog = EventStore.GetEventSequence(EventSequenceId.System);
            var mutationSequenceNumber = await systemLog.GetTailSequenceNumber().WaitAsync(timeout.Token);
            var reactor = await EventStore.Reactors.WaitForHandlerById(
                "$system.Cratis.Chronicle.EventSequences.EventSequencesReactor",
                TimeSpan.FromSeconds(30)).WaitAsync(timeout.Token);

            // Handling the mutation event completes only after the history change is stored.
            await reactor.WaitTillReachesEventSequenceNumber(mutationSequenceNumber, TimeSpan.FromSeconds(30)).WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException error) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException("The history mutation was not applied by the event sequences reactor within 30 seconds.", error);
        }
    }

    public async Task<int> StoredCount() => (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, Source)).Count;

    public override async Task DisposeAsync()
    {
        try
        {
            if (Unit?.IsCompleted == false)
            {
                await Unit.RollbackAsOwner(Owner);
            }
            Unit?.Dispose();
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}
