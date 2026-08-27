// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the command for seeding events into an event store.
/// </summary>
/// <param name="EventStore">The event store to seed into.</param>
/// <param name="GlobalByEventType">Global seed entries grouped by event type.</param>
/// <param name="GlobalByEventSource">Global seed entries grouped by event source.</param>
/// <param name="NamespacedEntries">Seed entries scoped to specific namespaces.</param>
[Command]
[BelongsTo(WellKnownServices.EventSeeding)]
public record SeedEvents(
    EventStoreName EventStore,
    IEnumerable<Contracts.Seeding.EventTypeSeedEntries> GlobalByEventType,
    IEnumerable<Contracts.Seeding.EventSourceSeedEntries> GlobalByEventSource,
    IEnumerable<Contracts.Seeding.NamespacedSeedEntries> NamespacedEntries)
{
    /// <summary>
    /// Handles the command by seeding the global and namespaced entries through their seeding grains.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get seeding grains with.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Handle(IGrainFactory grainFactory)
    {
        // The two groupings describe the same set of entries, so they are reconciled rather than concatenated.
        var globalEntries = SeedingConverters.Reconcile(
            GlobalByEventSource.SelectMany(_ => _.Entries),
            GlobalByEventType.SelectMany(_ => _.Entries));

        if (globalEntries.Count > 0)
        {
            await Seed(grainFactory, EventSeedingKey.ForGlobal(EventStore), globalEntries);
        }

        foreach (var namespacedGroup in NamespacedEntries)
        {
            var namespacedEntries = SeedingConverters.Reconcile(
                namespacedGroup.ByEventSource.SelectMany(_ => _.Entries),
                namespacedGroup.ByEventType.SelectMany(_ => _.Entries));

            if (namespacedEntries.Count > 0)
            {
                await Seed(grainFactory, EventSeedingKey.ForNamespace(EventStore, namespacedGroup.Namespace), namespacedEntries);
            }
        }
    }

    /// <summary>
    /// Seeds a batch of entries through the seeding grain for a key.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the grain with.</param>
    /// <param name="key">The <see cref="EventSeedingKey"/> identifying the grain.</param>
    /// <param name="entries">The entries to seed.</param>
    /// <returns>The <see cref="SeedingResult"/> the grain reports, which the caller deliberately drops.</returns>
    /// <remarks>
    /// The result is deliberately not propagated to the caller. A rejected seed batch is almost always a
    /// deterministic mistake in the seed set - a constraint violation - and this call sits on the client's
    /// connected path: the client seeds inside RegisterAll, which runs from OnConnected, and a failing handler
    /// there rolls the connection back to disconnected. The watchdog reconnects, the same seed set is rejected
    /// again, and a fixable mistake in a developer's seed data becomes a permanent client outage. The grain logs
    /// the rejection at Error with the violated constraints and leaves the batch unseeded, so a corrected run
    /// retries it.
    /// </remarks>
    static Task<SeedingResult> Seed(IGrainFactory grainFactory, EventSeedingKey key, IEnumerable<Contracts.Seeding.SeedingEntry> entries)
    {
        var grain = grainFactory.GetGrain<IResultAwareEventSeeding>(key.ToString());
        return grain.SeedWithResult(
        [
            .. entries.Select(_ => new SeedingEntry(
                _.EventSourceId,
                _.EventTypeId,
                _.Content,
                _.Tags?.Select(tag => new Concepts.Events.Tag(tag)).ToArray() ?? []))
        ]);
    }
}
