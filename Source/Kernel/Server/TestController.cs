// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Events.Store;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Server
{
    [Route("/api/test")]
    public class TestController : Controller
    {
        readonly IEventStore _eventStore;
        readonly IExecutionContextManager _executionContextManager;

        public TestController(IEventStore eventStore, IExecutionContextManager executionContextManager)
        {
            _eventStore = eventStore;
            _executionContextManager = executionContextManager;
        }

        [HttpGet]
        public async Task DoStuff()
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            await _eventStore.GetEventLog(Guid.Empty).Append(
                Guid.NewGuid().ToString(),
                new EventType(Guid.NewGuid(), 1),
                "{ \"blah\": 42 }"
            );
        }
    }
}
