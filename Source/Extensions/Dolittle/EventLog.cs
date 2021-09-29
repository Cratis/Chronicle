// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle
{
    [Route("/api/events/store/log/{eventLogId}")]
    public class EventLog : Controller
    {
        [HttpGet]
        public Task FindFor(
            [FromRoute] string eventLogId,
            [FromBody] EventFilter? filter)
        {
            return Task.CompletedTask;
        }

        [HttpGet("histogram")]
        public Task<IDictionary<DateTimeOffset, uint>> Histogram([FromRoute] string eventLogId)
        {
            var result = new Dictionary<DateTimeOffset, uint>();
            return Task.FromResult(result as IDictionary<DateTimeOffset, uint>);
        }

        [HttpGet("types")]
        public Task Types([FromRoute] string eventLogId)
        {
            return Task.CompletedTask;
        }
    }
}
