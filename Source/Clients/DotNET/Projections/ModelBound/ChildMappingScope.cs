// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Everything needed to map the members of a child type onto the definition of its children collection.
/// </summary>
/// <param name="Context">The shared processing collaborators.</param>
/// <param name="ChildrenDefinition">The children definition being populated.</param>
/// <param name="FromDefinition">The From definition of the event that creates the children.</param>
/// <param name="ChildType">The resolved item type of the children collection.</param>
/// <param name="CreatingEventType">The event type that creates the children.</param>
/// <param name="Identity">How the children are identified.</param>
/// <param name="Propagation">Which events are propagated into a self-referential collection.</param>
/// <param name="ShouldAutoMap">Whether auto-mapping is enabled for the children.</param>
internal sealed record ChildMappingScope(
    ChildrenProcessingContext Context,
    ChildrenDefinition ChildrenDefinition,
    FromDefinition FromDefinition,
    Type ChildType,
    Type CreatingEventType,
    ChildIdentity Identity,
    ChildPropagation Propagation,
    bool ShouldAutoMap)
{
    /// <summary>
    /// Determines whether an event declared on a member of the child type is propagated into the children definition.
    /// </summary>
    /// <param name="eventType">The event type declared on the member.</param>
    /// <returns><see langword="true"/> if the event is propagated; otherwise <see langword="false"/>.</returns>
    internal bool ShouldPropagate(Type eventType) =>
        ChildrenPropagationRules.ShouldPropagateChildMemberEvent(eventType, ChildType, Propagation.IncludeSelfReferencingEvents);
}
