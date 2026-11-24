// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// An immutable view of every event type in one event store at one <see cref="Revision"/>.
/// </summary>
/// <remarks>
/// Readers hold on to a snapshot for as long as they need a consistent view. A registration produces a new snapshot
/// with a higher revision and never changes one that has been handed out; entries that did not change are shared by
/// reference between consecutive snapshots.
/// </remarks>
/// <param name="Revision">The revision of the snapshot. Starts at 1 and increases by one every time the content changes.</param>
/// <param name="Entries">The <see cref="EventTypeEntry"/> of every event type, keyed by identifier.</param>
public sealed record EventTypeRegistrySnapshot(long Revision, FrozenDictionary<EventTypeId, EventTypeEntry> Entries)
{
    /// <summary>
    /// Gets the empty snapshot, which is where an event store starts before anything has been loaded for it.
    /// </summary>
    public static EventTypeRegistrySnapshot Empty { get; } = new(0, FrozenDictionary<EventTypeId, EventTypeEntry>.Empty);

    /// <summary>
    /// Gets the schema of the latest generation of every event type.
    /// </summary>
    public IReadOnlyCollection<EventTypeSchema> Latest { get; } = [.. Entries.Values.Select(_ => _.Latest)];

    /// <summary>
    /// Gets the definition of every event type.
    /// </summary>
    public IReadOnlyCollection<EventTypeDefinition> Definitions { get; } = [.. Entries.Values.Select(_ => _.Definition)];

    /// <summary>
    /// Try to get the entry of an event type.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/>.</param>
    /// <param name="entry">The <see cref="EventTypeEntry"/> when found.</param>
    /// <returns>True if found, false otherwise.</returns>
    public bool TryGetEntry(EventTypeId eventTypeId, out EventTypeEntry entry) => Entries.TryGetValue(eventTypeId, out entry!);
}
