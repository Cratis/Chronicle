// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Observation;
using Cratis.Events.Store;
using Cratis.Execution;
using Cratis.Extensions.GraphQL;

namespace Cratis.Server
{
    [GraphRoot("events/observations")]
    public class Observations : GraphController
    {
        readonly IEventStore _eventStore;
        readonly GetClusterClient _getClusterClient;

        public Observations(IEventStore eventStore, GetClusterClient getClusterClient)
        {
            _eventStore = eventStore;
            _getClusterClient = getClusterClient;
        }

        [Mutation]
        public async Task<bool>   Start()
        {
            var observer = new EventLogObserver();
            var clusterClient = _getClusterClient();
            var observerReference = await clusterClient.CreateObjectReference<IEventLogObserver>(observer);
            var observers = clusterClient.GetGrain<IEventLogObservers>(EventLogId.Default, keyExtension: ExecutionContextManager.GetCurrent().TenantId.ToString());
            await observers.Subscribe(observerReference);

            return true;
        }
    }
}
