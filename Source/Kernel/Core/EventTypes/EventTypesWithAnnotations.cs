// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Reports a stored generation carrying obsolete writer annotations.
/// </summary>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Generation">The stored generation.</param>
/// <param name="Keywords">The distinct annotation keywords found.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventTypes)]
public record EventTypesWithAnnotations(EventTypeId EventTypeId, EventTypeGeneration Generation, IEnumerable<string> Keywords)
{
    /// <summary>
    /// Reports affected generations without modifying storage.
    /// </summary>
    /// <param name="eventStore">The event store to inspect.</param>
    /// <param name="storage">The storage to inspect.</param>
    /// <returns>Only generations with cleanup annotations.</returns>
    public static async Task<IEnumerable<EventTypesWithAnnotations>> GetEventTypesWithAnnotations(EventStoreName eventStore, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).EventTypes.GetAllDefinitions();
        return definitions.SelectMany(definition => definition.Generations.Select(generation =>
            new EventTypesWithAnnotations(
                definition.Id,
                generation.Generation,
                JsonSchemaAnnotations.FindAnnotationKeywords(generation.Schema.Node).Select(item => item.Keyword).Distinct().Order().ToArray())))
            .Where(item => item.Keywords.Any()).ToArray();
    }
}
