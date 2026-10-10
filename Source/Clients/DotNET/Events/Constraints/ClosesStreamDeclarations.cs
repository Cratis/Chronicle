// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Shared declaration validation and same-name merging for closing constraints.
/// </summary>
internal static class ClosesStreamDeclarations
{
    /// <summary>
    /// The default event-source, stream-type and stream-identifier scope.
    /// </summary>
    internal const ClosedStreamDimensions DefaultDimensions = ClosedStreamDimensions.EventSourceId | ClosedStreamDimensions.EventStreamType | ClosedStreamDimensions.EventStreamId;

    /// <summary>
    /// Verify the serialized property exists on every closing and reopening type.
    /// </summary>
    /// <param name="eventTypes">The event types and schemas.</param>
    /// <param name="types">The participating event types.</param>
    /// <param name="property">The serialized property path, or null for metadata scoping.</param>
    /// <exception cref="PropertyDoesNotExistOnEventType">A participating type lacks the property.</exception>
    internal static void ValidateProperty(IEventTypes eventTypes, IEnumerable<Type> types, string? property)
    {
        if (property is null) return;
        foreach (var type in types)
        {
            var eventType = eventTypes.GetEventTypeFor(type);
            if (eventTypes.GetSchemaFor(eventType.Id).GetSchemaPropertyForPropertyPath(new PropertyPath(property)) is null)
            {
                throw new PropertyDoesNotExistOnEventType(eventType, property);
            }
        }
    }

    /// <summary>
    /// Merge compatible closing declarations under the same owner name.
    /// </summary>
    /// <param name="declarations">The declarations to merge.</param>
    /// <returns>The merged declarations, leaving other constraint types unchanged.</returns>
    /// <exception cref="ConflictingClosesStreamDeclarations">Same-named declarations disagree.</exception>
    /// <exception cref="MissingNameForClosesStreamConstraint">The owner name is reserved for manual closures.</exception>
    internal static IEnumerable<IConstraintDefinition> Merge(IEnumerable<IConstraintDefinition> declarations)
    {
        var merged = new List<IConstraintDefinition>();
        foreach (var declaration in declarations)
        {
            if (declaration is not ClosesStreamConstraintDefinition closing)
            {
                merged.Add(declaration);
                continue;
            }

            if (closing.Name.Value.Length == 0) throw new MissingNameForClosesStreamConstraint();
            var index = merged.FindIndex(candidate => candidate.Name == closing.Name);
            if (index < 0)
            {
                merged.Add(closing);
                continue;
            }

            if (merged[index] is not ClosesStreamConstraintDefinition existing || existing.Dimensions != closing.Dimensions || existing.EventStreamIdFrom != closing.EventStreamIdFrom)
            {
                throw new ConflictingClosesStreamDeclarations(closing.Name);
            }

            merged[index] = existing with
            {
                EventTypeIds = existing.EventTypeIds.Concat(closing.EventTypeIds).Distinct().ToArray(),
                ReopenedBy = existing.ReopenedBy.Concat(closing.ReopenedBy).Distinct().ToArray(),
                EventSequences = ConstraintEventSequences.Combine([existing.EventSequences, closing.EventSequences])
            };
        }

        var collision = merged.GroupBy(definition => definition.Name).FirstOrDefault(group => group.Count() > 1 && group.Any(definition => definition is ClosesStreamConstraintDefinition));
        if (collision is not null) throw new ConflictingClosesStreamDeclarations(collision.Key);

        return merged;
    }
}
