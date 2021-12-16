// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Extensions.Dolittle.Schemas.Api
{
    [Route("/api/events/types")]
    public class EventTypes : Controller
    {
        readonly ISchemaStore _schemaStore;

        public EventTypes(ISchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        [HttpGet]
        public async Task<IEnumerable<EventType>> AllEventTypes()
        {
            var schemas = await _schemaStore.GetLatestForAllEventTypes();

            return schemas.Select(_ =>
                new EventType(
                    _.EventType.Id.ToString(),
                    _.Schema.GetDisplayName(),
                    _.Schema.GetGeneration()));
        }

        [HttpGet("schemas/{eventTypeId}")]
        public async Task<IEnumerable<JsonDocument>> GenerationSchemasForType(
            [FromRoute] string eventTypeId)
        {
            var schemas = await _schemaStore.GetAllGenerationsForEventType(new global::Dolittle.SDK.Events.EventType(eventTypeId, 1));
            return schemas.Select(_ => JsonDocument.Parse(_.Schema.ToString()));
        }
    }
}
