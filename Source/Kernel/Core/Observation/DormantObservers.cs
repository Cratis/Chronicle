// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IDormantObservers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the appended events queues.</param>
/// <param name="records">The <see cref="IDurableSubscriptionRecords"/> to read the subscription from.</param>
[Singleton]
public class DormantObservers(IGrainFactory grainFactory, IDurableSubscriptionRecords records) : IDormantObservers
{
    /// <inheritdoc/>
    public async Task<ObserverDurableSubscription?> Find(ObserverKey observer)
    {
        if (!records.CanBeRelied)
        {
            return null;
        }

        var queues = grainFactory.GetGrain<IAppendedEventsQueues>(new EventSequenceKey(observer.EventSequenceId, observer.EventStore, observer.Namespace));
        if (!await queues.IsDormant(observer))
        {
            return null;
        }

        // The queue says it is dormant, so a record has to exist; one missing from a recent read was written after it.
        if (await records.Get(observer) is { } known)
        {
            return known;
        }

        var stored = await records.GetAll(observer.EventStore, observer.Namespace, observer.EventSequenceId, fresh: true);
        return stored.TryGetValue(observer.ObserverId, out var record) ? record : null;
    }
}
