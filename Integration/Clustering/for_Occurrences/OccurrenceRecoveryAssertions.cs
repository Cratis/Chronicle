// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Storage.Occurrences;

namespace Cratis.Chronicle.Integration.Clustering.for_Occurrences;

public sealed class OccurrenceRecoveryAssertions(IEventStore store, Contracts.Occurrences.IOccurrences service, string trigger)
{
    public async Task Recovered(string id, long previousEpoch, OccurrenceSnapshotResponse completed, string settlements)
    {
        completed.Epoch.ShouldEqual(previousEpoch + 1);
        completed.Receipts.Count().ShouldEqual(1);
        var events = await store.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, id).WaitAsync(TimeSpan.FromSeconds(30));
        Assert.True(events.Count == 1, $"Settlements: {settlements}. Events: {string.Join("; ", events.Select(e => $"{e.Context.SequenceNumber}:{e.Context.EventType.Id}:{e.Context.CorrelationId}:{e.Context.Occurred:O}"))}");
        var stale = await service.CompleteOccurrence(new()
        {
            EventStore = store.Name,
            Namespace = store.Namespace,
            Trigger = trigger,
            OccurrenceId = id,
            Epoch = previousEpoch
        }).EnsureSuccess().WaitAsync(TimeSpan.FromSeconds(30));
        stale.Status.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);
    }
}
