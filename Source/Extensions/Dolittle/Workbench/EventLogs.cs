// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle.Workbench
{
    public record EventLogInformation(string id, string name);

    [Route("/api/events/store/logs")]
    public class EventLogs : Controller
    {
        [HttpGet]
        public Task<IEnumerable<EventLogInformation>> AllEventLogs()
        {
            return Task.FromResult(new[] {
                new EventLogInformation("62748262-7fa0-4591-8b18-d74416807820", "Main Event Log")
            }.AsEnumerable());
        }
    }
}
