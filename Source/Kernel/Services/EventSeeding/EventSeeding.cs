// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSeeding;
using Cratis.Chronicle.Contracts.EventSeeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.EventSeeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
internal sealed class EventSeeding(IGrainFactory grainFactory) : IEventSeeding
{
    /// <inheritdoc/>
    public async Task<SeedResponse> Seed(SeedRequest request, CallContext context = default)
    {
        var key = new EventSeedingKey(request.EventStore, request.Namespace);
        var grain = grainFactory.GetGrain<Grains.EventSeeding.IEventSeeding>(key.ToString());

        var entries = request.Entries.Select(e => new Grains.EventSeeding.SeedingEntry(
            e.EventSourceId,
            e.EventTypeId,
            e.Content));

        await grain.Seed(entries);

        return new SeedResponse();
    }
}
