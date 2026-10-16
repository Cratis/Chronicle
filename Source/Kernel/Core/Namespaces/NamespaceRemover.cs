// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaceRemover"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for working with grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for the event stores.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
public class NamespaceRemover(
    IGrainFactory grainFactory,
    IStorage storage,
    ILogger<NamespaceRemover> logger) : INamespaceRemover
{
    /// <inheritdoc/>
    public async Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (@namespace == EventStoreNamespaceName.Default)
        {
            throw new NamespaceCannotBeRemoved(eventStore, @namespace);
        }

        logger.RemovingNamespace(eventStore, @namespace);
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(@namespace);

        // Every observer of the namespace is unsubscribed and removed. Removing an observer is what also removes its
        // reminders - left behind, they wake the observer at the next start and bring the namespace back.
        var definitions = (await eventStoreStorage.Observers.GetAll()).ToDictionary(_ => _.Identifier, _ => _.EventSequenceId);
        var fallbackSequences = (await namespaceStorage.GetEventSequences()).ToArray();
        foreach (var state in await namespaceStorage.Observers.GetAll())
        {
            foreach (var eventSequenceId in definitions.TryGetValue(state.Identifier, out var sequence) ? [sequence] : fallbackSequences)
            {
                var observer = grainFactory.GetGrain<IObserver>(new ObserverKey(state.Identifier, eventStore, @namespace, eventSequenceId));
                await observer.Unsubscribe();
                await observer.Remove();
            }
        }

        await eventStoreStorage.DropNamespace(@namespace);
        await eventStoreStorage.Namespaces.Delete(@namespace);
        await grainFactory.GetGrain<INamespaces>(eventStore).Forget(@namespace);

        logger.RemovedNamespace(eventStore, @namespace);
    }
}
