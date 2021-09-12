// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using Orleans;

namespace Cratis.Events.Store
{
    public delegate IClusterClient GetClusterClient();

    /// <summary>
    /// Represents an implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        readonly GetClusterClient _getClusterClient;

        public EventStore(GetClusterClient getClusterClient)
        {
            _getClusterClient = getClusterClient;
        }

        /// <inheritdoc/>
        public IEventLog DefaultEventLog => _getClusterClient().GetGrain<IEventLog>(EventLogId.Default, keyExtension: ExecutionContextManager.GetCurrent().TenantId.ToString());

        /// <inheritdoc/>
        public IEventLog PublicEventLog => _getClusterClient().GetGrain<IEventLog>(EventLogId.Public, keyExtension: ExecutionContextManager.GetCurrent().TenantId.ToString());

        /// <inheritdoc/>
        public IEventLog GetEventLog(EventLogId eventLogId) => _getClusterClient().GetGrain<IEventLog>(eventLogId, keyExtension: ExecutionContextManager.GetCurrent().TenantId.ToString());
    }
}
