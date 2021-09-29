// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.Dolittle.Schemas;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Schema;

namespace Cratis.Extensions.Dolittle
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
        public async Task<IEnumerable<EventType>> AllEvenTtypes()
        {
            var schemas = await _schemaStore.GetLatestForAllEventTypes();

            return schemas.Select(_ =>
                new EventType(
                    _.EventType.Id.ToString(),
                    _.Schema.GetDisplayName(),
                    _.Schema.GetGeneration()));
        }

        [HttpGet("schemas/{eventTypeId}")]
        public Task<IEnumerable<JSchema>> GenerationSchemasForType()
        {
            return Task.FromResult(new[] { new JSchema() }.AsEnumerable());
        }
    }
}
