// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage;

/// <summary>
/// Resolves an event reference once, when a projection declaration is compiled for registration.
/// </summary>
internal static class ProjectionEventTypeReference
{
    /// <summary>
    /// Resolves a bare name to the latest registered generation only in Pinned mode.
    /// </summary>
    /// <param name="reference">The bare event name accepted by Screenplay.</param>
    /// <param name="schemas">The registered event schemas.</param>
    /// <param name="generationDelivery">The generation delivery policy.</param>
    /// <returns>The event type and generation to persist.</returns>
    internal static EventType Resolve(string reference, IEnumerable<EventTypeSchema> schemas, EventGenerationDelivery generationDelivery)
    {
        var parsed = new EventType(reference, EventTypeGeneration.First);
        if (generationDelivery != EventGenerationDelivery.Pinned)
        {
            return parsed;
        }

        return schemas.Where(schema => schema.Type.Id == parsed.Id)
            .OrderByDescending(schema => schema.Type.Generation.Value)
            .Select(schema => schema.Type)
            .FirstOrDefault() ?? parsed;
    }
}
