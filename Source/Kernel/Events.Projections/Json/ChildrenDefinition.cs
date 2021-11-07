// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Json
{
    /// <summary>
    /// Represents the definition of a children projection.
    /// </summary>
    /// <param name="IdentifiedBy">Property on model that identifies the unique object, typically the key - or id (event source id).</param>
    /// <param name="Model">The target <see cref="ModelDefinition"/>.</param>
    /// <param name="From">All the <see cref="FromDefinition"/> for <see cref="EventType">event types</see>.</param>
    /// <param name="Children">All the <see cref="ChildrenDefinition"/> for properties on model.</param>
    /// <param name="RemovedWith">The definition of what removes a child, if any.</param>
    public record ChildrenDefinition(
        Property IdentifiedBy,
        ModelDefinition Model,
        IDictionary<EventType, FromDefinition> From,
        IDictionary<Property, ChildrenDefinition> Children,
        RemovedWithDefinition? RemovedWith) : ProjectionDefinition(Guid.Empty, Model, From, Children, RemovedWith);
}
