// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Observation;
using Cratis.Execution;
using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObservers"/>.
    /// </summary>
    public class Observers : Grain, IObservers
    {
        readonly IFailedObservers _failedObservers;
        readonly IExecutionContextManager _executionContextManager;
        readonly IGrainFactory _grainFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observers"/> class.
        /// </summary>
        /// <param name="failedObservers"><see cref="IFailedObservers"/> for getting all failed observers.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
        /// <param name="grainFactory"><see cref="IGrainFactory"/> for activating failed observers.</param>
        public Observers(
            IFailedObservers failedObservers,
            IExecutionContextManager executionContextManager,
            IGrainFactory grainFactory)
        {
            _failedObservers = failedObservers;
            _executionContextManager = executionContextManager;
            _grainFactory = grainFactory;
        }

        /// <inheritdoc/>
        public async Task RetryFailed()
        {
            // TODO: do this for for all tenants
            var tenant = "f455c031-630e-450d-a75b-ca050c441708";
            _executionContextManager.Establish(tenant, CorrelationId.New());

            var observers = await _failedObservers.GetAll();
            foreach (var observer in observers)
            {
                var key = PartitionedObserverKeyHelper.Create(tenant, observer.EventLogId, observer.EventSourceId);
                var partitionedObserver = _grainFactory.GetGrain<IPartitionedObserver>(observer.ObserverId, keyExtension: key);
                await partitionedObserver.TryResume();
            }
        }
    }
}
