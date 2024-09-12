// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents a key for a partitioned observer.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The event source.</param>
public record PartitionedObserverKey(EventStoreName EventStore, EventStoreNamespaceName Namespace, EventSequenceId EventSequenceId, EventSourceId EventSourceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="PartitionedObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="PartitionedObserverKey"/> to convert from.</param>
    public static implicit operator string(PartitionedObserverKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => KeyHelper.Combine(EventStore, Namespace, EventSequenceId, EventSourceId);

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="PartitionedObserverKey"/> instance.</returns>
    public static PartitionedObserverKey Parse(string key) => KeyHelper.Parse<PartitionedObserverKey>(key);

    /// <summary>
    /// Creates a <see cref="PartitionedObserverKey"/> based on an Observer key and the partition key.
    /// </summary>
    /// <param name="observer">The Observer Key.</param>
    /// <param name="partition">The <see cref="EventSourceId"/> to partition on.</param>
    /// <returns>A  <see cref="PartitionedObserverKey"/> instance.</returns>
    public static PartitionedObserverKey FromObserverKey(ObserverKey observer, EventSourceId partition)
        => new(observer.EventStore, observer.Namespace, observer.EventSequenceId, partition);
}
