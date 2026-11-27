// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Represents an implementation of <see cref="INamespaceRemover"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for working with grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for the event stores.</param>
/// <param name="reminderTable">The <see cref="IReminderTable"/> holding the persistent reminders of the observers.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
/// <param name="namespaceCasing">The namespace casing cache.</param>
/// <param name="incarnations">The optional opt-in lifecycle routing.</param>
public class NamespaceRemover(
    IGrainFactory grainFactory,
    IStorage storage,
    IReminderTable reminderTable,
    ILogger<NamespaceRemover> logger,
    INamespaceCasing namespaceCasing,
    INamespaceIncarnations? incarnations = null) : INamespaceRemover, INamespaceRemovalSteps
{
    const string ObserverGrainType = "observer";
    const int MaxRemovalAttempts = 5;
    static readonly TimeSpan _removalRetryDelay = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public async Task Remove(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        GuardDefaultNamespace(eventStore, @namespace);
        if (incarnations?.Enabled == true)
        {
            await incarnations.Remove(eventStore, @namespace);
            return;
        }
        logger.RemovingNamespace(eventStore, @namespace);
        await Execute(eventStore, @namespace, NamespaceRemovalStage.Forget);
        await Execute(eventStore, @namespace, NamespaceRemovalStage.Cleanup);
        await Execute(eventStore, @namespace, NamespaceRemovalStage.Drop);
        await Execute(eventStore, @namespace, NamespaceRemovalStage.DeleteListing);
        logger.RemovedNamespace(eventStore, @namespace);
    }

    /// <inheritdoc/>
    public Task Execute(EventStoreName eventStore, EventStoreNamespaceName @namespace, NamespaceRemovalStage stage)
    {
        GuardDefaultNamespace(eventStore, @namespace);
        return stage switch
        {
            NamespaceRemovalStage.Forget => Forget(eventStore, @namespace),
            NamespaceRemovalStage.Cleanup => Cleanup(eventStore, @namespace),
            NamespaceRemovalStage.Drop => storage.GetEventStore(eventStore).DropNamespace(@namespace),
            NamespaceRemovalStage.DeleteListing => DeleteListing(eventStore, @namespace),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "The lifecycle owner must handle this checkpoint.")
        };
    }

    /// <inheritdoc/>
    public async Task ForgetForIncarnation(EventStoreName eventStore, NamespaceLifecycle state)
    {
        GuardDefaultNamespace(eventStore, state.Namespace);
        if (NamespaceNameRules.ContainsComma(state.Namespace.Value)) await RemoveObservers(eventStore, state.Namespace);
        await grainFactory.GetGrain<INamespaces>(eventStore).ForgetIncarnation(state.Namespace, state.Incarnation, state.OperationId);
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

    static void GuardDefaultNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        // Storage may match names to an existing database regardless of case, so a case variant of the default
        // namespace stands for it as well.
        if (string.Equals(@namespace.Value, EventStoreNamespaceName.Default.Value, StringComparison.OrdinalIgnoreCase))
        {
            throw new NamespaceCannotBeRemoved(eventStore, @namespace);
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

    async Task Forget(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        // A comma-containing legacy name cannot activate once unlisted, even to remove its observers.
        if (NamespaceNameRules.ContainsComma(@namespace.Value))
        {
            await RemoveObservers(eventStore, @namespace);
        }

        // Ordinary names stop being listed before cleanup, preventing forwarding subscriptions from reappearing.
        await grainFactory.GetGrain<INamespaces>(eventStore).Forget(@namespace);
    }

    Task Cleanup(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        // Legacy observers cannot activate now, so remove any remaining reminders directly.
        return NamespaceNameRules.ContainsComma(@namespace.Value)
            ? RemoveRemindersOf(eventStore, @namespace)
            : RemoveObservers(eventStore, @namespace);
    }

    async Task DeleteListing(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        await storage.GetEventStore(eventStore).Namespaces.Delete(@namespace);
        namespaceCasing.Forget(eventStore, @namespace);
    }

    async Task RemoveObservers(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var namespaceStorage = eventStoreStorage.GetNamespace(@namespace);

        // Every observer of the namespace is unsubscribed and removed. Removing an observer is what also removes its
        // reminders - left behind, they wake the observer at the next start and bring the namespace back.
        var definitions = (await eventStoreStorage.Observers.GetAll()).ToDictionary(_ => _.Identifier, _ => _.EventSequenceId);
        var fallbackSequences = (await namespaceStorage.GetEventSequences()).ToArray();
        foreach (var state in await namespaceStorage.Observers.GetAll())
        {
            if (definitions.TryGetValue(state.Identifier, out var sequence))
            {
                await UnsubscribeAndRemove(grainFactory.GetGrain<IObserver>(new ObserverKey(state.Identifier, eventStore, @namespace, sequence)));
                continue;
            }

            foreach (var eventSequenceId in fallbackSequences)
            {
                await RemoveObserverWithoutDefinition(new ObserverKey(state.Identifier, eventStore, @namespace, eventSequenceId));
            }
        }

        // Observers that never saved any state - the forwarding observers between event stores and the kernel's own
        // pattern observers - are not found above, but they hold reminders all the same. Find them by their reminders.
        foreach (var (observerKey, _) in await GetObserverReminders(eventStore, @namespace))
        {
            await UnsubscribeAndRemove(grainFactory.GetGrain<IObserver>(observerKey));
        }
    }

    async Task RemoveObserverWithoutDefinition(ObserverKey key)
    {
        try
        {
            await UnsubscribeAndRemove(grainFactory.GetGrain<IObserver>(key));
        }
        catch (ObserverKeyDoesNotMatchDefinition)
        {
            logger.SkippingMismatchingObserverAddress(key);
        }
    }

    async Task RemoveRemindersOf(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        foreach (var (_, reminder) in await GetObserverReminders(eventStore, @namespace))
        {
            await reminderTable.RemoveRow(reminder.GrainId, reminder.ReminderName, reminder.ETag ?? string.Empty);
        }
    }

    async Task<IEnumerable<(ObserverKey Key, ReminderEntry Reminder)>> GetObserverReminders(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        // The table is read in two ranges because the first does not include a reminder hashed to zero.
        var rows = (await reminderTable.ReadRows(0, uint.MaxValue)).Reminders
            .Concat((await reminderTable.ReadRows(uint.MaxValue, 0)).Reminders);

        var reminders = new List<(ObserverKey Key, ReminderEntry Reminder)>();
        var keys = new HashSet<ObserverKey>();
        foreach (var row in rows)
        {
            if (row.GrainId.Type.ToString() != ObserverGrainType) continue;
            if (!TryParseKey(row.GrainId.Key.ToString(), out var key)) continue;
            if (key.EventStore == eventStore && key.Namespace == @namespace && keys.Add(key)) reminders.Add((key, row));
        }

        return reminders;
    }
}
