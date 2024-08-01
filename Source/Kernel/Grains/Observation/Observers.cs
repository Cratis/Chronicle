// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Jobs;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Observation;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>///
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains outside of Orleans task context.</param>
public class Observers(
    IStorage storage,
    IGrainFactory grainFactory) : Grain, IObservers
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        _ = this.GetPrimaryKeyLong(out var keyAsString);
        var key = ObserversKey.Parse(keyAsString!);

        var observerStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Observers;
        var observers = await observerStorage.GetAll();

        var observersForConsolidation = new List<ObserverKey>();

        foreach (var observerInfo in observers)
        {
            var observerKey = new ObserverKey(observerInfo.ObserverId, key.EventStore, key.Namespace, observerInfo.EventSequenceId);
            var observer = grainFactory.GetGrain<IObserver>(observerKey);
            await observer.Ensure();

            if ((observerInfo.Handled == EventCount.NotSet ||
                observerInfo.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable) &&
                observerInfo.EventTypes.Any())
            {
                observersForConsolidation.Add(observerKey);
            }
        }

        if (observersForConsolidation.Count == 0) return;

        var jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(key.EventStore, key.Namespace));
        await jobsManager.Start<IConsolidateStateForObservers, ConsolidateStateForObserveRequest>(
            JobId.New(),
            new ConsolidateStateForObserveRequest(observersForConsolidation));
    }
}
