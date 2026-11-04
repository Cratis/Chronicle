// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Naming;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the event stores a cluster-wide operation can reach, and the quarantined event stores and namespaces it had to leave out.
/// </summary>
/// <param name="Usable">The event stores that are not quarantined.</param>
/// <param name="SkippedResources">The quarantined event stores, and the quarantined namespaces of the usable ones, that were left out.</param>
/// <remarks>
/// A quarantined event store refuses to be opened, so an operation that fans out across every event store (patches,
/// erasure) must not try to open it: that would fail the whole operation, and for startup patches it would stop the
/// silo from starting. A quarantined namespace is dropped from the namespaces its event store reports, so an
/// operation that walks them reaches the rest without noticing it. Leaving a resource out is only half of the rule -
/// the caller decides how the operation reaches it once the quarantine is resolved.
/// </remarks>
public record UsableEventStores(IReadOnlyList<EventStoreName> Usable, SkippedResources SkippedResources)
{
    /// <summary>
    /// Gets whether any event store or namespace was left out.
    /// </summary>
    public bool HasSkipped => SkippedResources.Any;

    /// <summary>
    /// Lists the event stores of the installation, leaving out the quarantined ones and logging a warning for each.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to list.</param>
    /// <param name="resourceNames">The optional <see cref="IResourceNameSnapshot"/>. Without one, nothing is quarantined.</param>
    /// <param name="logger">The <see cref="ILogger"/> the skipped resources are reported to.</param>
    /// <param name="namespace">The optional namespace the operation is about. When given, only that namespace is checked; otherwise every namespace of a usable event store is.</param>
    /// <returns>The <see cref="UsableEventStores"/>.</returns>
    public static async Task<UsableEventStores> From(
        IStorage storage,
        IResourceNameSnapshot? resourceNames,
        ILogger logger,
        EventStoreNamespaceName? @namespace = null) =>
        await From(await storage.GetEventStores(), eventStore => NamespacesOf(storage, eventStore, @namespace), resourceNames, logger);

    /// <summary>
    /// Sorts the given event stores into usable and quarantined ones, leaving out the quarantined ones and logging a warning for each.
    /// </summary>
    /// <param name="eventStores">The event stores of the installation.</param>
    /// <param name="namespacesOf">Lists the namespaces of a usable event store. Only called when something is quarantined.</param>
    /// <param name="resourceNames">The optional <see cref="IResourceNameSnapshot"/>. Without one, nothing is quarantined.</param>
    /// <param name="logger">The <see cref="ILogger"/> the skipped resources are reported to.</param>
    /// <returns>The <see cref="UsableEventStores"/>.</returns>
    /// <remarks>
    /// For callers that list event stores and namespaces from the storage itself rather than through <see cref="IStorage"/>.
    /// </remarks>
    public static async Task<UsableEventStores> From(
        IEnumerable<EventStoreName> eventStores,
        Func<EventStoreName, Task<IEnumerable<EventStoreNamespaceName>>> namespacesOf,
        IResourceNameSnapshot? resourceNames,
        ILogger logger)
    {
        var usable = new List<EventStoreName>();
        var skipped = new List<EventStoreName>();
        var skippedNamespaces = new List<(EventStoreName EventStore, EventStoreNamespaceName Namespace)>();
        foreach (var eventStore in eventStores)
        {
            if (resourceNames?.IsQuarantined(eventStore) == true)
            {
                logger.LeavingOutQuarantinedEventStore(ResourceNameText.Escape(eventStore.Value));
                skipped.Add(eventStore);
                continue;
            }

            usable.Add(eventStore);
            if (resourceNames?.HasQuarantine != true)
            {
                continue;
            }

            skippedNamespaces.AddRange(QuarantinedNamespaces(eventStore, await namespacesOf(eventStore), resourceNames, logger));
        }

        return new(usable, new SkippedResources(skipped, skippedNamespaces));
    }

    static IEnumerable<(EventStoreName EventStore, EventStoreNamespaceName Namespace)> QuarantinedNamespaces(
        EventStoreName eventStore,
        IEnumerable<EventStoreNamespaceName> namespaces,
        IResourceNameSnapshot resourceNames,
        ILogger logger)
    {
        foreach (var name in namespaces.Where(name => resourceNames.IsQuarantined(eventStore, name)))
        {
            logger.LeavingOutQuarantinedNamespace(ResourceNameText.Escape(name.Value), ResourceNameText.Escape(eventStore.Value));
            yield return (eventStore, name);
        }
    }

    static async Task<IEnumerable<EventStoreNamespaceName>> NamespacesOf(IStorage storage, EventStoreName eventStore, EventStoreNamespaceName? @namespace) =>
        @namespace is { } named
            ? [named]
            : (await storage.GetEventStore(eventStore).Namespaces.GetAll()).Select(_ => _.Name);
}
