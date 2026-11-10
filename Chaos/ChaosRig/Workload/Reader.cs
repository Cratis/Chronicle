// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Collects event, observer, model, and ledger snapshots for the verdict.
/// </summary>
/// <param name="store">The isolated event store.</param>
/// <param name="ledger">The append intention ledger.</param>
/// <param name="sideLedger">The reactor delivery ledger.</param>
/// <param name="inspection">The persisted observer state reader.</param>
internal sealed class Reader(IEventStore store, Ledger ledger, SideLedger sideLedger, KernelObserverInspection inspection)
{
    static readonly string[] _expected = [ChaosReactor.Identifier, ChaosReducer.Identifier, ChaosProjection.Identifier];

    /// <summary>
    /// Collects one complete consistency snapshot.
    /// </summary>
    /// <param name="cancellation">The snapshot cancellation token.</param>
    /// <returns>The verdict input snapshot.</returns>
    public async Task<VerdictInput> Collect(CancellationToken cancellation)
    {
        var events = await Events(cancellation);
        var observers = (await store.Observers.GetAll().WaitAsync(cancellation)).Where(observer => _expected.Contains(observer.Id.Value))
            .Select(observer => new ObserverValue(observer.Id.Value, observer.LastHandledEventSequenceNumber.Value, observer.RunningState == ObserverRunningState.Active)).ToArray();
        var failed = (await store.FailedPartitions.GetAllFailedPartitions().WaitAsync(cancellation)).Count();
        var jobs = (await store.Jobs.GetJobs().WaitAsync(cancellation)).Count(job => job.Status is not (JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures));
        var state = await inspection.Read(_expected, cancellation);
        var (projections, reductions) = await Models(events, cancellation);
        return new(
            await ledger.Read().WaitAsync(cancellation),
            events,
            observers,
            _expected,
            failed,
            state.InFlight,
            jobs,
            projections,
            reductions,
            await sideLedger.Read().WaitAsync(cancellation),
            state.Documents);
    }

    static void EnsurePageAdvanced(EventSequenceNumber? current, EventSequenceNumber? next)
    {
        if (current is null) return;
        if (next is null) return;
        if (next.Value <= current.Value) throw new InvalidDataException("Event log pagination did not advance.");
    }

    async Task<IReadOnlyList<ObservedEvent>> Events(CancellationToken cancellation)
    {
        var result = new List<ObservedEvent>();
        EventSequenceNumber? next = null;
        do
        {
            var page = await store.EventLog.GetPage(EventSequencePageRequest.Forward(next, 1000)).WaitAsync(cancellation);
            foreach (var item in page.Events)
            {
                if (item.Content is not ChaosEvent contribution) throw new InvalidDataException("Unexpected event type in isolated chaos event log.");
                result.Add(new(contribution.LedgerId, item.Context.EventSourceId.Value, item.Context.SequenceNumber.Value, contribution.Payload));
            }
            EnsurePageAdvanced(next, page.Next);
            next = page.Next;
        }
        while (next is not null);
        return result;
    }

    async Task<(IReadOnlyList<ModelValue> Projections, IReadOnlyList<ModelValue> Reductions)> Models(IReadOnlyList<ObservedEvent> events, CancellationToken cancellation)
    {
        var projections = new List<ModelValue>();
        var reductions = new List<ModelValue>();
        foreach (var source in events.Select(item => item.Source).Distinct())
        {
            var projection = await store.ReadModels.GetInstanceById<ChaosProjectionModel>(source).WaitAsync(cancellation);
            var reduction = await store.ReadModels.GetInstanceById<ChaosReductionModel>(source).WaitAsync(cancellation);
            if (projection is not null) projections.Add(new(source, projection.Count, projection.Sum));
            if (reduction is not null) reductions.Add(new(source, reduction.Count, reduction.Sum));
        }
        return (projections, reductions);
    }
}
