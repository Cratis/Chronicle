// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Validates the <see cref="ChildrenOrdering"/> declared for a children collection when a projection is created.
/// </summary>
internal static class ChildrenOrderingValidation
{
    /// <summary>
    /// Validates an ordering, throwing <see cref="InvalidChildrenOrdering"/> when it cannot be honored.
    /// </summary>
    /// <param name="projectionId">The <see cref="ProjectionId"/> of the projection declaring the ordering.</param>
    /// <param name="childrenProperty">The <see cref="PropertyPath"/> of the children collection.</param>
    /// <param name="ordering">The optional <see cref="ChildrenOrdering"/>.</param>
    /// <param name="childSchema">The optional <see cref="JsonSchema"/> of the child read model.</param>
    /// <exception cref="InvalidChildrenOrdering">Thrown when the ordering is not valid for the children collection.</exception>
    internal static void Validate(ProjectionId projectionId, PropertyPath childrenProperty, ChildrenOrdering? ordering, JsonSchema? childSchema)
    {
        if (ordering is null)
        {
            return;
        }

        if (!Enum.IsDefined(ordering.Placement))
        {
            throw new InvalidChildrenOrdering(projectionId, childrenProperty, $"'{(int)ordering.Placement}' is not a known placement - expected one of {string.Join(", ", Enum.GetNames<ChildrenPlacement>())}.");
        }

        if (!Enum.IsDefined(ordering.Direction))
        {
            throw new InvalidChildrenOrdering(projectionId, childrenProperty, $"'{(int)ordering.Direction}' is not a known sort direction - expected one of {string.Join(", ", Enum.GetNames<SortDirection>())}.");
        }

        if (ordering.Placement == ChildrenPlacement.Sorted)
        {
            ValidateSortProperty(projectionId, childrenProperty, ordering, childSchema);
        }
    }

    static void ValidateSortProperty(ProjectionId projectionId, PropertyPath childrenProperty, ChildrenOrdering ordering, JsonSchema? childSchema)
    {
        if (ordering.SortBy?.IsSet != true)
        {
            throw new InvalidChildrenOrdering(projectionId, childrenProperty, "a sorted ordering needs a child property to sort by.");
        }

        var childProperties = childSchema?.GetFlattenedProperties().Select(_ => _.Name).ToArray() ?? [];
        if (childProperties.Length == 0)
        {
            return;
        }

        var sortProperty = childSchema!.GetSchemaPropertyForPropertyPath(ordering.SortBy) ?? throw new InvalidChildrenOrdering(
            projectionId,
            childrenProperty,
            $"the children are sorted by '{ordering.SortBy}', which the child read model does not have (available properties: {string.Join(", ", childProperties)}).");

        var sortType = sortProperty.ActualTypeSchema.Type;
        if (sortType.HasFlag(JsonObjectType.Object) || sortType.HasFlag(JsonObjectType.Array))
        {
            throw new InvalidChildrenOrdering(
                projectionId,
                childrenProperty,
                $"the children are sorted by '{ordering.SortBy}', which is {(sortType.HasFlag(JsonObjectType.Array) ? "a collection" : "an object")} - children can only be sorted by a single value such as a number, string, date, Guid or boolean.");
        }
    }
}
