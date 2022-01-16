// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Events.Store.Grains;
using Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;

#pragma warning disable SA1600, IDE0060

namespace Cratis.Events.Store.Api
{
    /// <summary>
    /// Represents the API for working with the event log.
    /// </summary>
    [Route("/api/events/store/log")]
    public class EventLog : Controller
    {
        readonly IGrainFactory _grainFactory;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLog"/> class.
        /// </summary>
        /// <param name="grainFactory"><see cref="IGrainFactory"/>.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
        public EventLog(
            IGrainFactory grainFactory,
            IExecutionContextManager executionContextManager)
        {
            _grainFactory = grainFactory;
            _executionContextManager = executionContextManager;
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
            return Task.CompletedTask;
        }

        [HttpGet("{eventLogId}/types")]
        public Task Types([FromRoute] string eventLogId)
        {
            return Task.CompletedTask;
        }
    }
}
