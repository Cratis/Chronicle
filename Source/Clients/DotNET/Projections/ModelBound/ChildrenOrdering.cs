// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Resolves and applies the ordering declared on a <c language="csharp">[ChildrenFrom]</c> attribute.
/// </summary>
static class ChildrenOrdering
{
    /// <summary>
    /// Resolves the ordering declared on a ChildrenFrom attribute.
    /// </summary>
    /// <param name="attribute">The attribute to read the ordering from.</param>
    /// <param name="childType">The item type of the collection, used to validate sort properties; or <see langword="null"/>.</param>
    /// <param name="memberName">The member name, used when reporting a conflict.</param>
    /// <param name="namingPolicy">The naming policy for converting property names.</param>
    /// <returns>The declared ordering, or <see langword="null"/> when the attribute declares none.</returns>
    /// <exception cref="ConflictingChildrenOrdering">Thrown when the attribute declares more than one ordering.</exception>
    internal static ChildrenOrderingDefinition? Declared(Attribute attribute, Type? childType, string memberName, INamingPolicy namingPolicy)
    {
        if (attribute is not IChildrenFromAttribute childrenFrom)
        {
            return null;
        }

        var declared = new List<ChildrenOrderingDefinition>();
        if (childrenFrom.Prepend)
        {
            declared.Add(new() { Placement = ChildrenPlacement.Prepend });
        }

        AddSorted(declared, childrenFrom.OrderBy, SortDirection.Ascending, childType, namingPolicy);
        AddSorted(declared, childrenFrom.OrderByDescending, SortDirection.Descending, childType, namingPolicy);

        return declared.Count > 1 ? throw new ConflictingChildrenOrdering(memberName) : declared.FirstOrDefault();
    }

    /// <summary>
    /// Applies a declared ordering to a children definition.
    /// </summary>
    /// <param name="childrenDef">The children definition.</param>
    /// <param name="ordering">The declared ordering, if any.</param>
    /// <param name="memberName">The member name, used when reporting a conflict.</param>
    /// <exception cref="ConflictingChildrenOrdering">Thrown when the definition already has a different ordering.</exception>
    internal static void Apply(ChildrenDefinition childrenDef, ChildrenOrderingDefinition? ordering, string memberName)
    {
        if (ordering is null)
        {
            return;
        }

        if (childrenDef.Ordering is not null && !IsSameOrdering(childrenDef.Ordering, ordering))
        {
            throw new ConflictingChildrenOrdering(memberName);
        }

        childrenDef.Ordering = ordering;
    }

    static void AddSorted(List<ChildrenOrderingDefinition> declared, string? propertyName, SortDirection direction, Type? childType, INamingPolicy namingPolicy)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        if (childType is not null)
        {
            PropertyValidator.ValidatePropertyExists(childType, propertyName);
        }

        declared.Add(new()
        {
            Placement = ChildrenPlacement.Sorted,
            SortBy = (string)ProjectionPropertyPaths.Resolve(propertyName, namingPolicy, ProjectionPathRole.Target),
            Direction = direction
        });
    }

    static bool IsSameOrdering(ChildrenOrderingDefinition left, ChildrenOrderingDefinition right) =>
        left.Placement == right.Placement && left.SortBy == right.SortBy && left.Direction == right.Direction;
}
