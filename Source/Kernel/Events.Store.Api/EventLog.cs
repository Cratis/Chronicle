// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cratis.Events.Store.Api
{
    [Route("/api/events/store/log")]
    public class EventLog : Controller
    {
        readonly ILogger<EventLog> _logger;

        public EventLog(ILogger<EventLog> logger)
        {
            _logger = logger;
        }

        [HttpGet("{eventLogId}/{eventSourceId}")]
        public Task FindFor(
            [FromRoute] EventLogId eventLogId,
            [FromRoute] EventSourceId eventSourceId)
        {
            _logger.LogInformation($"Find {eventLogId}- {eventSourceId}");
            return Task.CompletedTask;
        }
    }
}
