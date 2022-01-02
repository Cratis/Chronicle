// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Events.Store.Observation
{
    /// <summary>
    /// Helper class for working with the key for a partitioned obsever.
    /// </summary>
    public static class PartitionedObserverKeyHelper
    {
        /// <summary>
        /// Create the key.
        /// </summary>
        /// <param name="tenantId">Tenant component.</param>
        /// <param name="eventSourceId">Event source component.</param>
        /// <returns>Key.</returns>
        public static string Create(TenantId tenantId, EventSourceId eventSourceId) => $"{tenantId}+{eventSourceId}";

        /// <summary>
        /// Parse a key into its components.
        /// </summary>
        /// <param name="key">Key to parse.</param>
        /// <returns>Tuple with tenant and event source.</returns>
        public static (TenantId tenantId, EventSourceId eventSourceId) Parse(string key)
        {
            var index = key.IndexOf("+", StringComparison.InvariantCulture);
            var tenantId = (TenantId)key.Substring(0, index);
            var eventSourceId = (EventSourceId)key.Substring(index + 1);
            return (tenantId, eventSourceId);
        }
    }
}
