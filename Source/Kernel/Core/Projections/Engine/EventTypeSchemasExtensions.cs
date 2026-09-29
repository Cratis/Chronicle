// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Extension methods for resolving the schema a projection uses for an event type it projects.
/// </summary>
public static class EventTypeSchemasExtensions
{
    /// <summary>
    /// Find the schema to use for an event type a projection projects from.
    /// </summary>
    /// <param name="schemas">The available <see cref="EventTypeSchema">schemas</see>.</param>
    /// <param name="eventType">The <see cref="EventType"/> the projection references.</param>
    /// <returns>The matching <see cref="EventTypeSchema"/>, or null if the event type has no schema.</returns>
    /// <remarks>
    /// Events reach a projection by event type identifier, whatever generation the projection definition names, and
    /// carry the content of the highest generation they have. An exact match on the event type - including its
    /// generation - is used when present; otherwise the highest generation available for the same event type
    /// identifier is used, so a projection definition naming another generation than the one stored still resolves
    /// the schema of the content it receives.
    /// </remarks>
    public static EventTypeSchema? SchemaFor(this IEnumerable<EventTypeSchema> schemas, EventType eventType)
    {
        EventTypeSchema? latestForIdentifier = null;
        foreach (var schema in schemas)
        {
            if (schema.Type.Id != eventType.Id)
            {
                continue;
            }

            if (schema.Type.Generation == eventType.Generation)
            {
                return schema;
            }

            if (latestForIdentifier is null || schema.Type.Generation.Value > latestForIdentifier.Type.Generation.Value)
            {
                latestForIdentifier = schema;
            }
        }

        return latestForIdentifier;
    }
}
