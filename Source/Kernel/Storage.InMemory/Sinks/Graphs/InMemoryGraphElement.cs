// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Storage.InMemory.Sinks.Graphs;

/// <summary>
/// Represents the stored state shared by nodes and edges in the <see cref="InMemoryGraphSink"/>.
/// </summary>
public class InMemoryGraphElement
{
    /// <summary>
    /// Gets or sets the properties.
    /// </summary>
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets the event sequence number of the last write, or null when no event has written it yet.
    /// </summary>
    public ulong? Watermark { get; set; }

    /// <summary>
    /// Gets or sets the projection that owns the element, or null when only an edge created it.
    /// </summary>
    public ProjectionId? Owner { get; set; }

    /// <summary>
    /// Gets or sets the replay generation of the owner the element was last written in.
    /// </summary>
    public long Generation { get; set; }

    /// <summary>
    /// Copy the state of another element onto this one.
    /// </summary>
    /// <param name="other">The element to copy from.</param>
    public void CopyFrom(InMemoryGraphElement other)
    {
        Properties = new Dictionary<string, object?>(other.Properties);
        Watermark = other.Watermark;
        Owner = other.Owner;
        Generation = other.Generation;
    }

    /// <summary>
    /// Make a copy of the state.
    /// </summary>
    /// <returns>The copy.</returns>
    public InMemoryGraphElement Snapshot()
    {
        var copy = new InMemoryGraphElement();
        copy.CopyFrom(this);
        return copy;
    }
}
