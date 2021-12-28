// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Events.Store.Grains;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Cratis.Server
{
    [Route("/api/test")]
    public class TestController : Controller
    {
        readonly IGrainFactory _grainFactory;
        readonly IExecutionContextManager _executionContextManager;

        public TestController(IGrainFactory grainFactory, IExecutionContextManager executionContextManager)
        {
            _grainFactory = grainFactory;
            _executionContextManager = executionContextManager;
        }

        [HttpGet]
        public async Task DoStuff()
        {
            _executionContextManager.Establish(
                Guid.Parse("f455c031-630e-450d-a75b-ca050c441708"),
                Guid.NewGuid().ToString()
            );

            var eventLog = _grainFactory.GetGrain<IEventLog>(Guid.Empty, keyExtension: "f455c031-630e-450d-a75b-ca050c441708");
            await eventLog.Append(
                Guid.NewGuid().ToString(),
                new EventType(Guid.NewGuid(), 1),
                "{ \"blah\": 42 }"
            );
        }
    }
}
