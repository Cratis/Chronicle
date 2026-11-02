// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Optional capability of a sink that keeps applied event markers for <see cref="SinkWriteMode.OncePerSourceEvent"/>.
/// </summary>
public interface ISinkAppliedEventMarkers
{
    /// <summary>
    /// Clears the markers the events of a partition left, so that a partition replay rewrites the partition.
    /// </summary>
    /// <remarks>Idempotent: clearing twice is harmless.</remarks>
    /// <param name="namespace">The event store namespace.</param>
    /// <param name="partition">The partition (event source id) that is replayed.</param>
    /// <param name="joins">The root join definitions of the projection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAppliedEventMarkers(EventStoreNamespaceName @namespace, Key partition, IReadOnlyList<AppliedEventMarkerJoin> joins);
}
