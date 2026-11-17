// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Extension methods for applying <see cref="PropertyDifference"/> instances to a child.
/// </summary>
internal static class ChildDifferenceExtensions
{
    /// <summary>
    /// Apply a difference recorded for a child to the child, addressed by the last segment of its path.
    /// </summary>
    /// <param name="difference">The difference to apply.</param>
    /// <param name="child">The child to apply it to.</param>
    internal static void ApplyToChild(this PropertyDifference difference, object child) =>
        Apply(PropertyPath.CreateFrom([difference.PropertyPath.LastSegment]), difference, child, difference.ArrayIndexers);

    /// <summary>
    /// Apply a difference to a child that is being added, addressed relative to the collection holding it.
    /// </summary>
    /// <param name="difference">The difference to apply.</param>
    /// <param name="child">The child being added.</param>
    /// <param name="childrenProperty">The property holding the children, stripped from the path when the difference starts with it.</param>
    /// <param name="arrayIndexers">Indexers selecting the child.</param>
    internal static void ApplyToNewChild(this PropertyDifference difference, object child, PropertyPath childrenProperty, ArrayIndexers arrayIndexers) =>
        Apply(RelativeToChildren(difference.PropertyPath, childrenProperty), difference, child, arrayIndexers);

    static PropertyPath RelativeToChildren(PropertyPath differencePath, PropertyPath childrenProperty)
    {
        var childSegments = childrenProperty.Segments.ToArray();
        var differenceSegments = differencePath.Segments.ToArray();
        var hasCollectionPrefix = differenceSegments.Length > childSegments.Length &&
            differenceSegments.Take(childSegments.Length).Select(segment => segment.Value)
                .SequenceEqual(childSegments.Select(segment => segment.Value));
        return PropertyPath.CreateFrom(hasCollectionPrefix
            ? differenceSegments.Skip(childSegments.Length).ToArray()
            : differenceSegments);
    }

    static void Apply(PropertyPath propertyPath, PropertyDifference difference, object child, ArrayIndexers arrayIndexers)
    {
        if (difference.IsRemoval)
        {
            propertyPath.RemoveValue(child, arrayIndexers);
            return;
        }

        propertyPath.SetValue(child, difference.Changed!, arrayIndexers);
    }
}
