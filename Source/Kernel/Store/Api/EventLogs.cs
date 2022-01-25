// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Events.Store.Api
{
    /// <summary>
    /// Represents the API for working with event logs.
    /// </summary>
    [Route("/api/events/store/logs")]
    public class EventLogs : Controller
    {
        /// <summary>
        /// Gets all event logs.
        /// </summary>
        /// <returns>Collection of event logs.</returns>
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
