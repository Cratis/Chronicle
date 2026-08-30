// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents the command for adding a single seed entry.
/// </summary>
/// <param name="EventStore">The event store to seed into.</param>
/// <param name="Namespace">The namespace to seed into, ignored when the entry is global.</param>
/// <param name="EventSourceId">The event source the seeded event belongs to.</param>
/// <param name="EventTypeId">The type of the seeded event.</param>
/// <param name="Content">The JSON content of the seeded event.</param>
/// <param name="IsGlobal">Whether the entry applies to every namespace rather than one.</param>
[Command]
[BelongsTo(WellKnownServices.EventSeeding)]
public record AddSeedEntry(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSourceId EventSourceId,
    EventTypeId EventTypeId,
    string Content,
    bool IsGlobal)
{
    /// <summary>
    /// Handles the command by seeding the single entry through its seeding grain.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the seeding grain with.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IGrainFactory grainFactory)
    {
        var key = IsGlobal
            ? EventSeedingKey.ForGlobal(EventStore)
            : EventSeedingKey.ForNamespace(EventStore, Namespace);

        var grain = grainFactory.GetGrain<IResultAwareEventSeeding>(key.ToString());

        // The result is dropped for the same reason SeedEvents drops it - see the note there.
        await grain.SeedWithResult([new SeedingEntry(EventSourceId, EventTypeId, Content, [])]);
    }
}
