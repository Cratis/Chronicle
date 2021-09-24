// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/store/log")]
    public class EventLog : Controller
    {
        [HttpGet("{eventLogId}/{eventSourceId}")]
        public Task FindFor(
            [FromRoute] string eventLogId,
            [FromRoute] string eventSourceId)
        {
            return Task.CompletedTask;
        }
    }
}
