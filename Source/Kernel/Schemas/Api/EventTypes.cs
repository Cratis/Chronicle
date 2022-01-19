// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Events.Schemas.API
{
    /// <summary>
    /// Represents the API for working with event types.
    /// </summary>
    [Route("/api/events/types")]
    public class EventTypes : Controller
    {
        readonly ISchemaStore _schemaStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTypes"/> class.
        /// </summary>
        /// <param name="schemaStore">Underlying <see cref="ISchemaStore"/>.</param>
        public EventTypes(ISchemaStore schemaStore)
        {
            _schemaStore = schemaStore;
        }

        /// <summary>
        /// Gets all event types.
        /// </summary>
        /// <returns>Collection of event types.</returns>
        [HttpGet]
        public async Task<IEnumerable<EventType>> AllEventTypes()
        {
            var schemas = await _schemaStore.GetLatestForAllEventTypes();

            return schemas.Select(_ =>
                new EventType(
                    _.Type.Id.ToString(),
                    _.Schema.GetDisplayName(),
                    _.Schema.GetGeneration()));
        }

        /// <summary>
        /// Gets generation schema for type.
        /// </summary>
        /// <param name="eventTypeId">Type to get for.</param>
        /// <returns>Schemas.</returns>
        [HttpGet("schemas/{eventTypeId}")]
        public async Task<IEnumerable<JsonDocument>> GenerationSchemasForType(
            [FromRoute] string eventTypeId)
        {
            var schemas = await _schemaStore.GetAllGenerationsForEventType(new (eventTypeId, 1));
            return schemas.Select(_ => JsonDocument.Parse(_.Schema.ToJson()));
        }
    }
}
