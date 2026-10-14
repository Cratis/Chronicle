// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Compares seed identity independently of its provenance and tag sequence reference.
/// </summary>
internal sealed class SeededEventEntryIdentity : IEqualityComparer<SeededEventEntry>
{
    /// <summary>
    /// The shared seed identity comparer.
    /// </summary>
    internal static readonly SeededEventEntryIdentity Comparer = new();

    /// <inheritdoc/>
    public bool Equals(SeededEventEntry? x, SeededEventEntry? y) =>
        ReferenceEquals(x, y) ||
        (x is not null &&
         y is not null &&
         x.EventSourceId == y.EventSourceId &&
         x.EventTypeId == y.EventTypeId &&
         SeedRouting.SourceType(x.EventSourceType?.Value) == SeedRouting.SourceType(y.EventSourceType?.Value) &&
         SeedRouting.StreamType(x.EventStreamType?.Value) == SeedRouting.StreamType(y.EventStreamType?.Value) &&
         SeedRouting.StreamId(x.EventStreamId?.Value) == SeedRouting.StreamId(y.EventStreamId?.Value) &&
         string.Equals(x.Content, y.Content, StringComparison.Ordinal) &&
         TagsOf(x).SetEquals(TagsOf(y)));

    /// <inheritdoc/>
    public int GetHashCode(SeededEventEntry obj) =>
        HashCode.Combine(obj.EventSourceId, obj.EventTypeId, obj.Content, TagsOf(obj).Count, SeedRouting.SourceType(obj.EventSourceType?.Value), SeedRouting.StreamType(obj.EventStreamType?.Value), SeedRouting.StreamId(obj.EventStreamId?.Value));

    static HashSet<string> TagsOf(SeededEventEntry entry) => new(entry.Tags ?? [], StringComparer.Ordinal);
}
