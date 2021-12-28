// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store;
using Cratis.Events.Store.Grains;
using Cratis.Execution;
using Orleans;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        readonly IClusterClient _clusterClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStore"/> class.
        /// </summary>
        /// <param name="clusterClient"><see cref="IClusterClient"/> for working with Orleans.</param>
        /// <param name="eventTypes">The <see cref="IEventTypes"/> in the system.</param>
        public EventStore(IClusterClient clusterClient, IEventTypes eventTypes)
        {
            _clusterClient = clusterClient;

             var defaultEventLog = _clusterClient.GetGrain<IEventLog>(EventLogId.Default, keyExtension: ExecutionContextManager.GetCurrent().TenantId.ToString());
             DefaultEventLog = new ClientEventLog(eventTypes, defaultEventLog);
        }

        /// <inheritdoc/>
        public IClientEventLog DefaultEventLog { get; }
    }
}
