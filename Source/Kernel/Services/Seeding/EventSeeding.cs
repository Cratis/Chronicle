// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Contracts.Seeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
internal sealed class EventSeeding(IGrainFactory grainFactory) : IEventSeeding
{
    /// <inheritdoc/>
    public async Task Seed(SeedRequest request, CallContext context = default)
    {
        var key = new EventSeedingKey(request.EventStore, request.Namespace);
        var grain = grainFactory.GetGrain<Grains.Seeding.IEventSeeding>(key.ToString());

        var entries = request.Entries.Select(e => new Grains.Seeding.SeedingEntry(
            e.EventSourceId,
            e.EventTypeId,
            e.Content,
            e.Tags?.Select(t => new Concepts.Events.Tag(t)) ?? [])).ToArray();

        await grain.Seed(entries);
    }
}
