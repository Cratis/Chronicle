// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Rules deciding which events declared on a child type are propagated into its children collection.
/// </summary>
static class ChildrenPropagationRules
{
    /// <summary>
    /// Collects the class-level <see cref="FromEventAttribute{TEvent}"/> attributes of a child type that are
    /// propagated into its children collection.
    /// </summary>
    /// <param name="childType">The item type of the children collection.</param>
    /// <param name="creatingEventType">The event type that creates the children.</param>
    /// <param name="propagation">Which events are propagated into a self-referential collection.</param>
    /// <returns>The attributes to apply to every member of the child type.</returns>
    internal static List<Attribute> CollectClassLevelFromEvents(Type childType, Type creatingEventType, ChildPropagation propagation) =>
        childType.GetCustomAttributes()
            .Where(attribute => attribute.IsClosedFormOf(typeof(FromEventAttribute<>)) &&
                                ShouldPropagateClassLevelFromEvent(attribute, childType, creatingEventType, propagation))
            .ToList();

    /// <summary>
    /// Determines whether an event declared on a member of the child type is propagated into the children definition.
    /// </summary>
    /// <param name="eventType">The event type declared on the member.</param>
    /// <param name="childType">The item type of the children collection, if known.</param>
    /// <param name="includeSelfReferencingEvents">Whether events that also create children of the item type are propagated.</param>
    /// <returns><see langword="true"/> if the event is propagated; otherwise <see langword="false"/>.</returns>
    internal static bool ShouldPropagateChildMemberEvent(Type eventType, Type? childType, bool includeSelfReferencingEvents) =>
        includeSelfReferencingEvents || childType is null || !ChildTypeInspection.HasChildrenFromForEventType(childType, eventType);

    /// <summary>
    /// Determines whether a class-level <see cref="FromEventAttribute{TEvent}"/> is propagated into a self-referential
    /// children collection.
    /// </summary>
    /// <param name="fromEventAttribute">The class-level attribute.</param>
    /// <param name="childCreatingEventType">The event type that creates the children.</param>
    /// <param name="ancestorCreatorEventTypes">Events that create children of an ancestor collection.</param>
    /// <returns><see langword="true"/> if the attribute is propagated; otherwise <see langword="false"/>.</returns>
    internal static bool ShouldPropagateForSelfReference(
        Attribute fromEventAttribute,
        Type childCreatingEventType,
        IReadOnlySet<Type>? ancestorCreatorEventTypes)
    {
        var childLevelEventType = fromEventAttribute.GetType().GetGenericArguments()[0];

        // Always propagate if it's the child-creating event itself
        if (childLevelEventType == childCreatingEventType)
        {
            return true;
        }

        // Never propagate an event that creates children of an ANCESTOR collection (e.g. the event that adds
        // top-level features) into a descendant self-referential collection. Doing so would materialize the
        // ancestor's instances as (self-)children of the descendant.
        if (ancestorCreatorEventTypes?.Contains(childLevelEventType) == true)
        {
            return false;
        }

        // Keyed update events without explicit parent keys should not be propagated in self-referential cases,
        // as they would be upserted as self-children when resolved via EventSourceId fallback.
        return !HasExplicitKey(fromEventAttribute) || !string.IsNullOrEmpty(fromEventAttribute.ReadString(nameof(FromEventAttribute<object>.ParentKey)));
    }

    static bool ShouldPropagateClassLevelFromEvent(Attribute attribute, Type childType, Type creatingEventType, ChildPropagation propagation) =>
        propagation.IncludeSelfReferencingEvents
            ? ShouldPropagateForSelfReference(attribute, creatingEventType, propagation.AncestorCreatorEventTypes)
            : ShouldPropagateForRegularCollection(attribute, childType);

    static bool ShouldPropagateForRegularCollection(Attribute fromEventAttribute, Type childType)
    {
        if (fromEventAttribute is IKeyedAttribute { Key.Length: > 0 })
        {
            return true;
        }

        var eventType = fromEventAttribute.GetType().GetGenericArguments()[0];
        return !ChildTypeInspection.HasChildrenFromForEventType(childType, eventType);
    }

    static bool HasExplicitKey(Attribute fromEventAttribute) =>
        !string.IsNullOrEmpty(fromEventAttribute.ReadString(nameof(FromEventAttribute<object>.Key)));
}
