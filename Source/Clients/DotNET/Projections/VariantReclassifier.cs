// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Turns an ordinary projection definition into one variant of a mutually exclusive group, shared by the
/// model-bound and fluent authoring paths so both express variants the same way.
/// </summary>
/// <remarks>
/// A variant compiles to an ordinary, independent projection definition. What makes it a variant is which of its
/// handlers may create it: only the entering event(s) keep their create-or-update <c language="csharp">From</c>
/// handler, and everything else becomes an update-only, self-referential <c language="csharp">Join</c> keyed by
/// the variant's own key member. A join never creates a document, so no other event can create the variant or
/// resurrect the entity into one it has since left. Mutual exclusion is then expressed with the ordinary
/// <c language="csharp">RemovedWith</c> mechanism, against every sibling's entering event.
/// </remarks>
internal static class VariantReclassifier
{
    /// <summary>
    /// Reclassifies every handler that is not an entering event into an update-only, self-referential join.
    /// </summary>
    /// <param name="from">The create-or-update handlers, modified in place.</param>
    /// <param name="join">The update-only handlers, modified in place.</param>
    /// <param name="enteringEventTypes">The event types that activate this variant.</param>
    /// <param name="keyPropertyName">The variant's own key member, used as the join's correlation property.</param>
    internal static void Reclassify(
        IDictionary<EventType, FromDefinition> from,
        IDictionary<EventType, JoinDefinition> join,
        IEnumerable<EventType> enteringEventTypes,
        string keyPropertyName)
    {
        var entering = enteringEventTypes.ToList();

        foreach (var eventType in from.Keys.Where(candidate => !entering.Exists(candidate.IsSameEventType)).ToList())
        {
            var fromDefinition = from[eventType];
            from.Remove(eventType);
            join[eventType] = new JoinDefinition
            {
                On = keyPropertyName,
                Key = fromDefinition.Key ?? WellKnownExpressions.EventSourceId,
                Properties = fromDefinition.Properties
            };
        }
    }

    /// <summary>
    /// Adds the removal entries that make the variants of a group mutually exclusive.
    /// </summary>
    /// <param name="removedWith">The removal handlers, modified in place.</param>
    /// <param name="siblingEnteringEventTypes">The entering event types of every other variant in the group.</param>
    internal static void AddMutualExclusion(IDictionary<EventType, RemovedWithDefinition> removedWith, IEnumerable<EventType> siblingEnteringEventTypes)
    {
        foreach (var siblingEventType in siblingEnteringEventTypes)
        {
            if (removedWith.Keys.Any(siblingEventType.IsSameEventType))
            {
                continue;
            }

            removedWith[siblingEventType] = new RemovedWithDefinition
            {
                Key = WellKnownExpressions.EventSourceId,
                ParentKey = WellKnownExpressions.EventSourceId
            };
        }
    }

    /// <summary>
    /// Makes the variants of each identity mutually exclusive, now that every variant in a group is known.
    /// </summary>
    /// <param name="definitions">The built definitions, by declaring type.</param>
    /// <param name="variantDeclarations">What each projection declared about being a variant.</param>
    /// <remarks>
    /// A variant can only be told what removes it once its siblings are known, which is not true while any one of
    /// them is being built. Reclassifying a variant's own handlers is self-contained and has already happened by
    /// the time this runs.
    /// </remarks>
    internal static void CrossWireGroups(
        IDictionary<Type, ProjectionDefinition> definitions,
        IDictionary<Type, FluentVariantDeclaration> variantDeclarations)
    {
        foreach (var group in variantDeclarations.GroupBy(declaration => declaration.Value.Identity))
        {
            var membersOfGroup = group.ToList();

            foreach (var (declaringType, _) in membersOfGroup)
            {
                if (!definitions.TryGetValue(declaringType, out var definition))
                {
                    continue;
                }

                var siblingEnteringEventTypes = membersOfGroup
                    .Where(sibling => sibling.Key != declaringType)
                    .SelectMany(sibling => sibling.Value.EnteringEventTypes);

                AddMutualExclusion(definition.RemovedWith, siblingEnteringEventTypes);
            }
        }
    }

    /// <summary>
    /// Compares two event types by value. The contract type has reference equality, and a variant's entering
    /// events and its definition's keys do not necessarily come from the same instance cache.
    /// </summary>
    /// <param name="eventType">The event type to compare.</param>
    /// <param name="other">The event type to compare against.</param>
    /// <returns>True if both describe the same event type; otherwise, false.</returns>
    static bool IsSameEventType(this EventType eventType, EventType other) =>
        eventType.Id == other.Id &&
        eventType.Generation == other.Generation &&
        eventType.Tombstone == other.Tombstone;
}
