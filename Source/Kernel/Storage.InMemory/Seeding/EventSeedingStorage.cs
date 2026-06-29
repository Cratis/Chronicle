// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Storage.InMemory.Seeding;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventSeedingStorage"/>.
/// </summary>
public sealed class EventSeedingStorage : IEventSeedingStorage
{
    EventSeeds _seeds = new(
        new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
        new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

    /// <inheritdoc/>
    public Task<EventSeeds> Get() => Task.FromResult(_seeds);

    /// <inheritdoc/>
    public Task Save(EventSeeds data)
    {
        _seeds = data;
        return Task.CompletedTask;
    }
}
