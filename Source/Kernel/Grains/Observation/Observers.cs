// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Jobs;
using Cratis.Kernel.Grains.Jobs;
using Cratis.Kernel.Grains.Observation.Jobs;
using Cratis.Kernel.Storage;
using Cratis.Observation;

namespace Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : Grain, IObservers
{
    readonly IStorage _storage;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>///
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains outside of Orleans task context.</param>
    public Observers(
        IStorage storage,
        IGrainFactory grainFactory)
    {
        _storage = storage;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        _ = this.GetPrimaryKeyLong(out var keyAsString);
        var key = ObserversKey.Parse(keyAsString!);

        var observerStorage = _storage.GetEventStore((string)key.MicroserviceId).GetNamespace(key.TenantId).Observers;
        var observers = await observerStorage.GetAllObservers();

        var observersForConsolidation = new List<ObserverIdAndKey>();

        foreach (var observerInfo in observers)
        {
            var observerKey = new ObserverKey(key.MicroserviceId, key.TenantId, observerInfo.EventSequenceId);
            var observer = _grainFactory.GetGrain<IObserver>(observerInfo.ObserverId, keyExtension: observerKey);
            await observer.Ensure();

            if ((observerInfo.Handled == EventCount.NotSet ||
                observerInfo.LastHandledEventSequenceNumber == EventSequenceNumber.Unavailable) &&
                observerInfo.EventTypes.Any())
            {
                observersForConsolidation.Add(new ObserverIdAndKey(observerInfo.ObserverId, observerKey));
            }
        }

        if (observersForConsolidation.Count == 0) return;

        var jobsManager = GrainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(key.MicroserviceId, key.TenantId));
        await jobsManager.Start<IConsolidateStateForObservers, ConsolidateStateForObserveRequest>(
            JobId.New(),
            new ConsolidateStateForObserveRequest(observersForConsolidation));
    }
}
