// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Boot;
using Cratis.Execution;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Represents a <see cref="IPerformBootProcedure"/> for the event store.
    /// </summary>
    public class BootProcedure : IPerformBootProcedure
    {
        readonly GetClusterClient _getClusterClient;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BootProcedure"/> class.
        /// </summary>
        /// <param name="getClusterClient"></param>
        /// <param name="executionContextManager"></param>
        public BootProcedure(GetClusterClient getClusterClient, IExecutionContextManager executionContextManager)
        {
            _getClusterClient = getClusterClient;
            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            var eventLog = _getClusterClient().GetGrain<IEventLog>(EventLogId.Default, keyExtension: "f455c031-630e-450d-a75b-ca050c441708");
            eventLog.WarmUp();
        }
    }
}
