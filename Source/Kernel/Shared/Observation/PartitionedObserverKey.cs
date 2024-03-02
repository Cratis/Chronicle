// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a key for a partitioned observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The event source.</param>
public record PartitionedObserverKey(MicroserviceId MicroserviceId, TenantId TenantId, EventSequenceId EventSequenceId, EventSourceId EventSourceId)
{
    /// <summary>
    /// Implicitly convert from <see cref="PartitionedObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="PartitionedObserverKey"/> to convert from.</param>
    public static implicit operator string(PartitionedObserverKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString() => $"{MicroserviceId}+{TenantId}+{EventSequenceId}+{EventSourceId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="PartitionedObserverKey"/> instance.</returns>
    public static PartitionedObserverKey Parse(string key)
    {
        var elements = key.Split('+');
        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventSequenceId = (EventSequenceId)elements[2];
        var eventSourceId = (EventSourceId)elements[3];
        return new(microserviceId, tenantId, eventSequenceId, eventSourceId);
    }

    /// <summary>
    /// Creates a <see cref="PartitionedObserverKey"/> based on an Observer key and the partition key.
    /// </summary>
    /// <param name="observer">The Observer Key.</param>
    /// <param name="partition">The <see cref="EventSourceId"/> to partition on.</param>
    /// <returns>A  <see cref="PartitionedObserverKey"/> instance.</returns>
    public static PartitionedObserverKey FromObserverKey(ObserverKey observer, EventSourceId partition)
        => new(observer.MicroserviceId, observer.TenantId, observer.EventSequenceId, partition);
}
