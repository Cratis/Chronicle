// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Boot;
using Cratis.Execution;
using Orleans;

namespace Cratis.Events.Store.Grains
{
    /// <summary>
    /// Represents a <see cref="IPerformBootProcedure"/> for the event store.
    /// </summary>
    public class BootProcedure : IPerformBootProcedure
    {
        readonly IGrainFactory _grainFactory;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootProcedure"/> class.
        /// </summary>
        /// <param name="grainFactory"></param>
        /// <param name="executionContextManager"></param>
        public BootProcedure(IGrainFactory grainFactory, IExecutionContextManager executionContextManager)
        {
            _grainFactory = grainFactory;
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            var eventLog = _grainFactory.GetGrain<IEventLog>(EventLogId.Default, keyExtension: "f455c031-630e-450d-a75b-ca050c441708");
            eventLog.WarmUp();
        }
    }
}
