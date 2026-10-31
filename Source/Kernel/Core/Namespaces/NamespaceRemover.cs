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
/// <param name="reminderTable">The <see cref="IReminderTable"/> holding the persistent reminders of the observers.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
public class NamespaceRemover(
    IGrainFactory grainFactory,
    IStorage storage,
    IReminderTable reminderTable,
    ILogger<NamespaceRemover> logger) : INamespaceRemover
{
    const string ObserverGrainType = "observer";
    const int MaxRemovalAttempts = 5;
    static readonly TimeSpan _removalRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public async Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        if (@namespace == EventStoreNamespaceName.Default)
        {
            throw new NamespaceCannotBeRemoved(eventStore, @namespace);
        }

        logger.RemovingNamespace(eventStore, @namespace);

        // The namespace stops being listed first. The subscriptions between event stores re-subscribe the forwarding
        // observers of every listed namespace once a minute, which would otherwise subscribe an observer again
        // between it being unsubscribed and removed.
        await grainFactory.GetGrain<INamespaces>(eventStore).Forget(@namespace);
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
                await UnsubscribeAndRemove(grainFactory.GetGrain<IObserver>(new ObserverKey(state.Identifier, eventStore, @namespace, eventSequenceId)));
            }
        }

        // Observers that never saved any state - the forwarding observers between event stores and the kernel's own
        // pattern observers - are not found above, but they hold reminders all the same. Find them by their reminders.
        foreach (var observerKey in await GetObserversWithReminders(eventStore, @namespace))
        {
            await UnsubscribeAndRemove(grainFactory.GetGrain<IObserver>(observerKey));
        }

        await eventStoreStorage.DropNamespace(@namespace);
        await eventStoreStorage.Namespaces.Delete(@namespace);

        logger.RemovedNamespace(eventStore, @namespace);
    }

    /// <summary>
    /// Try to parse the key of an observer grain.
    /// </summary>
    /// <param name="value">The key to parse.</param>
    /// <param name="key">The parsed <see cref="ObserverKey"/>.</param>
    /// <returns>True if the key was an observer key; otherwise false.</returns>
    internal static bool TryParseKey(string value, out ObserverKey key)
    {
        try
        {
            key = ObserverKey.Parse(value);
            return true;
        }
        catch (Exception)
        {
            key = ObserverKey.NotSet;
            return false;
        }
    }

    static async Task UnsubscribeAndRemove(IObserver observer)
    {
        for (var attempt = 1; ; attempt++)
        {
            await observer.Unsubscribe();
            try
            {
                await observer.Remove();
                return;
            }
            catch (ObserverRemovalNotAllowed) when (attempt < MaxRemovalAttempts)
            {
                // Subscribed again by a subscription that was already on its way; unsubscribe once more.
                await Task.Delay(_removalRetryDelay);
            }
        }
    }

    async Task<IEnumerable<ObserverKey>> GetObserversWithReminders(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        // The table is read in two ranges because the first does not include a reminder hashed to zero.
        var rows = (await reminderTable.ReadRows(0, uint.MaxValue)).Reminders
            .Concat((await reminderTable.ReadRows(uint.MaxValue, 0)).Reminders);

        var keys = new HashSet<ObserverKey>();
        foreach (var row in rows)
        {
            if (row.GrainId.Type.ToString() != ObserverGrainType) continue;
            if (!TryParseKey(row.GrainId.Key.ToString(), out var key)) continue;
            if (key.EventStore == eventStore && key.Namespace == @namespace) keys.Add(key);
        }

        return keys;
    }
}
