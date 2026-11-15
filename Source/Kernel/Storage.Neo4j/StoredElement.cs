// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Neo4j;

/// <summary>
/// Represents the stored state of a node or an edge: the properties, bookkeeping included, and the edges attached to a node.
/// </summary>
/// <param name="Properties">All properties as stored.</param>
/// <param name="Edges">The number of edges attached to the element, for a node.</param>
public record StoredElement(IReadOnlyDictionary<string, object> Properties, long Edges = 0)
{
    /// <summary>
    /// Gets the sequence number of the last event that wrote the element, or null when only an edge has created it.
    /// </summary>
    public long? Watermark => Properties.TryGetValue(GraphBookkeeping.Watermark, out var value) ? Convert.ToInt64(value) : null;

    /// <summary>
    /// Gets the projection that owns the element, or null when only an edge has created it.
    /// </summary>
    public string? Owner => Properties.TryGetValue(GraphBookkeeping.Owner, out var value) ? value as string : null;

    /// <summary>
    /// Gets the replay generation of the owner the element was last written in.
    /// </summary>
    public long Generation => Properties.TryGetValue(GraphBookkeeping.Generation, out var value) ? Convert.ToInt64(value) : 0;

    /// <summary>
    /// Gets whether the element was written during an earlier replay generation than the replay <paramref name="reader"/> is in.
    /// </summary>
    /// <param name="reader">The owner that reads, which only sees its own elements as stale.</param>
    /// <param name="replayGeneration">The replay generation of the reader, or null when it is not replaying.</param>
    /// <returns>True when the element reads as absent for the reader.</returns>
    public bool IsStale(string reader, long? replayGeneration) =>
        Owner is { } owner && owner == reader && replayGeneration is { } replay && Generation < replay;

    /// <summary>
    /// Gets whether the last write to the element was at or after a sequence number, which makes a write for it redundant.
    /// </summary>
    /// <param name="sequence">The event sequence number of the write.</param>
    /// <returns>True when the write is skipped.</returns>
    public bool IsAtOrBelowWatermark(long sequence) => Watermark is { } watermark && sequence <= watermark;
}
