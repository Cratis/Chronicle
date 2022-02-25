// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Represents a key for a partitioned observer.
/// </summary>
/// <param name="MicroserviceId">The Microservice identifier.</param>
/// <param name="TenantId">The Tenant identifier.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The event source.</param>
public record PartitionedObserverKey(MicroserviceId MicroserviceId, TenantId TenantId, EventSequenceId EventSequenceId, EventSourceId EventSourceId)
{
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
}
