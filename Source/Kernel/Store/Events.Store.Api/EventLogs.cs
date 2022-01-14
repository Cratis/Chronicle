// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Events.Store.Api
{
    [Route("/api/events/store/logs")]
    public class EventLogs : Controller
    {
        [HttpGet]
        public Task<IEnumerable<EventLogInformation>> AllEventLogs()
        {
            return Task.FromResult(new[]
            {
                new EventLogInformation("00000000-0000-0000-0000-000000000000", "Main Event Log")
            }.AsEnumerable());
        }
    }
}
