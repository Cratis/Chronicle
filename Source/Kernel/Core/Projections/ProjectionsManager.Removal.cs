// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

public partial class ProjectionsManager
{
    /// <inheritdoc/>
    public async Task<ObserverRemovalResult> Remove(ProjectionId identifier)
    {
        var eventStoreStorage = storage.GetEventStore(_eventStoreName);
        var definition = State.Projections.FirstOrDefault(projection => projection.Identifier == identifier);
        if (definition is null)
        {
            if (!await eventStoreStorage.Projections.Has(identifier)) return ObserverRemovalResult.NotFound;
            definition = await eventStoreStorage.Projections.Get(identifier);
        }

        if (definition.IsKernelOwned) return ObserverRemovalResult.Active(EventStoreNamespaceName.NotSet);
        if (await ProjectionDeclarations.IsDeclared(GrainFactory, _eventStoreName, identifier))
        {
            return ObserverRemovalResult.Subscribed(EventStoreNamespaceName.NotSet);
        }

        // Registration holds the same non-reentrant turn as removal. Timers and namespace reconciliation
        // deliberately interleave, so fence new work and drain captured passes before deleting anything.
        // Keep the fence on failure: a retry completes cleanup; only an explicit registration revives it.
        _removingProjections.Add(identifier);
        await Task.WhenAll(_subscriptionPasses.ToArray());
        var namespaces = await GrainFactory.GetGrain<INamespaces>(_eventStoreName).GetAll();
        foreach (var namespaceName in namespaces)
        {
            await StopObserverFor(definition, namespaceName);
            await GrainFactory.GetGrain<IObserver>(new ObserverKey(identifier, _eventStoreName, namespaceName, definition.EventSequenceId)).Remove();
        }

        await eventStoreStorage.Observers.Delete(identifier);
        await Forget(identifier);
        await eventStoreStorage.Projections.Delete(identifier);
        return ObserverRemovalResult.Removed;
    }
}
