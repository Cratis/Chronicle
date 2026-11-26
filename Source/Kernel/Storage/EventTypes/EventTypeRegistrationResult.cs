// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Describes a conditional registration, including the generations this caller actually created.
/// </summary>
/// <param name="Changed">Whether stored data changed.</param>
/// <param name="AddedGenerations">The generations inserted by this caller.</param>
/// <param name="Conflict">The existing generation with a different schema, if any.</param>
/// <param name="EventTypeAlreadyStored">Whether the event type had stored generations at the conditional write.</param>
public record EventTypeRegistrationResult(bool Changed, IReadOnlyList<EventTypeGeneration> AddedGenerations, EventTypeGeneration? Conflict = null, bool EventTypeAlreadyStored = false)
{
    /// <summary>
    /// Compares an insert-only registration with authoritative stored generations.
    /// </summary>
    /// <param name="incoming">The generations expected to be absent.</param>
    /// <param name="stored">The currently stored generations.</param>
    /// <returns>The generations still absent, or the first conflicting generation.</returns>
    public static EventTypeRegistrationResult Compare(EventTypeDefinition incoming, IEnumerable<EventTypeGenerationDefinition> stored)
    {
        var existing = stored.ToDictionary(_ => _.Generation);
        var added = new List<EventTypeGeneration>();
        foreach (var generation in incoming.Generations)
        {
            if (!existing.TryGetValue(generation.Generation, out var previous))
            {
                added.Add(generation.Generation);
            }
            else if (!JsonSchemaCompatibilityExtensions.EqualsIgnoringAnnotations(previous.Schema.ToJson(), generation.Schema.ToJson()))
            {
                return new(false, [], generation.Generation);
            }
        }

        return new(false, added, EventTypeAlreadyStored: existing.Count > 0);
    }
}
