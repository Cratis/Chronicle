// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
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
    /// <param name="eventSequence">The event sequence the projection reads from.</param>
    /// <param name="partition">The partition (event source id) that is replayed.</param>
    /// <param name="joins">The root join definitions of the projection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearAppliedEventMarkers(EventStoreNamespaceName @namespace, EventSequenceId eventSequence, Key partition, IReadOnlyList<AppliedEventMarkerJoin> joins);

    /// <summary>
    /// Applies changes with <see cref="SinkWriteMode.OncePerSourceEvent"/>, recording the markers under the identity of the event sequence the event was read from.
    /// </summary>
    /// <remarks>
    /// Two projections can maintain one read model from different event sequences. Their events can carry equal event source ids and
    /// sequence numbers, so the identity of the sequence is part of every marker stream.
    /// </remarks>
    /// <param name="key">Key of the read model to upsert.</param>
    /// <param name="changeset">The changes to apply.</param>
    /// <param name="eventSequenceNumber">The sequence number of the event that caused the changes.</param>
    /// <param name="origin">Where the event was read from and how the write is to be confirmed.</param>
    /// <returns>A collection of <see cref="FailedPartition"/> if any partitions failed.</returns>
    Task<IEnumerable<FailedPartition>> ApplyChangesOncePerSourceEvent(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, EventSequenceNumber eventSequenceNumber, AppliedEventMarkerWriteOrigin origin);
}
