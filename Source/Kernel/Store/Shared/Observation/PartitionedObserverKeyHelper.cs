// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Events.Store.Observation;

/// <summary>
/// Helper class for working with the key for a partitioned obsever.
/// </summary>
public static class PartitionedObserverKeyHelper
{
    /// <summary>
    /// Create the key.
    /// </summary>
    /// <param name="microserviceId">Microservice component.</param>
    /// <param name="tenantId">Tenant component.</param>
    /// <param name="eventLogId">The event log it is for.</param>
    /// <param name="eventSourceId">Event source component.</param>
    /// <returns>Key.</returns>
    public static string Create(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventLogId, EventSourceId eventSourceId) => $"{microserviceId}-{tenantId}+{eventLogId}+{eventSourceId}";

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Tuple with tenant, event log and event source.</returns>
    public static (MicroserviceId MicroserviceId, TenantId TenantId, EventSequenceId EventLogId, EventSourceId EventSourceId) Parse(string key)
    {
        var elements = key.Split('+');

        var microserviceId = (MicroserviceId)elements[0];
        var tenantId = (TenantId)elements[1];
        var eventLogId = (EventSequenceId)elements[2];
        var eventSourceId = (EventSourceId)elements[3];
        return (microserviceId, tenantId, eventLogId, eventSourceId);
    }
}
