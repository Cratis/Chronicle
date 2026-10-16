// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventTypes;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.Reactors.Kernel;
using Cratis.Chronicle.Projections.Kernel;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Serializes store initialization across the cluster, independently of storage existence.
/// </summary>
/// <param name="storage">The storage containing the durable initialization marker.</param>
/// <param name="eventTypes">The server event types.</param>
/// <param name="reactors">The kernel reactors.</param>
/// <param name="kernelProjections">The kernel projections.</param>
public class EventStoreInitializer(IStorage storage, IEventTypes eventTypes, IReactors reactors, IKernelProjections kernelProjections) : Grain, IEventStoreInitializer
{
    /// <inheritdoc/>
    public async Task Ensure()
    {
        var name = (EventStoreName)this.GetPrimaryKeyString();
        var store = storage.GetEventStore(name);

        // Keep server types and reactors current on reconnect, as before.
        await eventTypes.DiscoverAndRegister(name);
        await reactors.DiscoverAndRegister(name, EventStoreNamespaceName.Default);
        await GrainFactory.GetGrain<INamespaces>(name).EnsureDefault();

        // Read the durable marker, not projection-definition caches. A missing marker also repairs stores
        // implicitly created before this fix. This non-reentrant grain serializes concurrent ensures on all silos.
        if (await store.IsInitialized()) return;

        await kernelProjections.DiscoverAndRegister(name);
        var result = await GrainFactory.GetSystemEventSequence(EventStoreName.System).Append(name.Value, new EventStoreAdded(name));
        if (!result.IsSuccess)
        {
            throw new EventStoreInitializationFailed(name);
        }

        // Only successful setup is marked complete. Failed or interrupted attempts remain retryable;
        // a crash before this write can repeat setup, but never leaves a false completion marker.
        await store.MarkInitialized();
    }
}
