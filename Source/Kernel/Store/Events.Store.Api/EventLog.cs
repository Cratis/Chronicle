// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Events.Store.Grains;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Cratis.Events.Store.Api
{
    [Route("/api/events/store/log")]
    public class EventLog : Controller
    {
        readonly IGrainFactory _grainFactory;
        readonly IExecutionContextManager _executionContextManager;
        readonly ILogger<EventLog> _logger;

        public EventLog(
            IGrainFactory grainFactory,
            IExecutionContextManager executionContextManager,
            ILogger<EventLog> logger)
        {
            _grainFactory = grainFactory;
            _executionContextManager = executionContextManager;
            _logger = logger;
        }

        [HttpPost("{eventSourceId}/{eventTypeId}/{eventGeneration}")]
        public async Task Append(
            [FromRoute] EventSourceId eventSourceId,
            [FromRoute] EventTypeId eventTypeId,
            [FromRoute] EventGeneration eventGeneration)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var eventLog = _grainFactory.GetGrain<IEventLog>(EventLogId.Default, keyExtension: _executionContextManager.Current.TenantId.ToString());
            await eventLog.Append(
                eventSourceId,
                new EventType(eventTypeId, eventGeneration),
                content);
        }

        [HttpGet("{eventLogId}")]
        public Task<IEnumerable<Event>> FindFor([FromRoute] string eventLogId) => Task.FromResult(Array.Empty<Event>().AsEnumerable());


        [HttpGet("{eventLogId}/count")]
        public Task<long> Count() => Task.FromResult(0L);

        [HttpGet("histogram")]
        public Task<IEnumerable<EventHistogramEntry>> Histogram([FromRoute] string eventLogId) => Task.FromResult(Array.Empty<EventHistogramEntry>().AsEnumerable());

        [HttpGet("{eventLogId}/{eventSourceId}")]
        public Task FindFor(
            [FromRoute] EventLogId eventLogId,
            [FromRoute] EventSourceId eventSourceId)
        {
            _logger.LogInformation($"Find {eventLogId}- {eventSourceId}");
            return Task.CompletedTask;
        }

        [HttpGet("{eventLogId}/types")]
        public Task Types([FromRoute] string eventLogId)
        {
            return Task.CompletedTask;
        }
    }
}
