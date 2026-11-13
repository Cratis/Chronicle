// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Explicitly removes obsolete writer annotations from existing stored generations.
/// </summary>
/// <param name="EventStore">The event store to normalize.</param>
/// <param name="EventTypeIds">Optional identifiers to restrict normalization to. Null or empty selects all types.</param>
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record NormalizeEventTypeSchemas(EventStoreName EventStore, IEnumerable<EventTypeId>? EventTypeIds = null)
{
    /// <summary>
    /// Rewrites only affected schemas, without registering generations or modifying events.
    /// </summary>
    /// <param name="storage">The storage holding the schemas.</param>
    /// <param name="eventTypesCacheClient">Client for evicting caches on every silo.</param>
    /// <returns>The generations actually changed and the keywords removed.</returns>
    public async Task<NormalizeEventTypeSchemasResult> Handle(IStorage storage, IEventTypesCacheClient eventTypesCacheClient)
    {
        var eventTypes = storage.GetEventStore(EventStore).EventTypes;
        var selected = EventTypeIds?.ToHashSet() ?? [];
        var definitions = await eventTypes.GetAllDefinitions();
        var changed = new List<EventTypesWithAnnotations>();
        foreach (var definition in definitions.Where(item => selected.Count == 0 || selected.Contains(item.Id)))
        {
            var changedType = false;
            try
            {
                foreach (var generation in definition.Generations)
                {
                    var keywords = JsonSchemaAnnotations.FindAnnotationKeywords(generation.Schema.Node).Select(item => item.Keyword).Distinct().Order().ToArray();
                    if (keywords.Length > 0 && await eventTypes.ReplaceSchema(definition.Id, generation.Generation, new JsonSchema((JsonObject)JsonSchemaAnnotations.RemoveAnnotations(generation.Schema.Node))))
                    {
                        changed.Add(new(definition.Id, generation.Generation, keywords));
                        changedType = true;
                    }
                }
            }
            finally
            {
                // Evict even if a later generation fails after an earlier replacement was persisted.
                if (changedType)
                {
                    await eventTypesCacheClient.Invalidate(EventStore, definition.Id);
                }
            }
        }
        return new(changed);
    }
}
