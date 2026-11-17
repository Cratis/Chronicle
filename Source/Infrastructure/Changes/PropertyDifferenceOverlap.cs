// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Decides how property differences that overlap on a property path are merged.
/// </summary>
internal static class PropertyDifferenceOverlap
{
    /// <summary>
    /// Check whether two differences address the same array elements.
    /// </summary>
    /// <param name="left">The first difference.</param>
    /// <param name="right">The second difference.</param>
    /// <returns>True if their array indexers are equal.</returns>
    internal static bool HasSameArrayIndexers(PropertyDifference left, PropertyDifference right) =>
        left.ArrayIndexers.Equals(right.ArrayIndexers);

    /// <summary>
    /// Check whether a property path is the same as, or below, a candidate parent path.
    /// </summary>
    /// <param name="propertyPath">The path to check.</param>
    /// <param name="candidateParentPath">The candidate parent path.</param>
    /// <returns>True if <paramref name="candidateParentPath"/> is set and is a prefix of <paramref name="propertyPath"/>.</returns>
    internal static bool IsSameAsOrDescendantOf(PropertyPath propertyPath, PropertyPath candidateParentPath)
    {
        if (!candidateParentPath.IsSet)
        {
            return false;
        }

        var pathSegments = propertyPath.Segments.Select(_ => _.Value).ToArray();
        var candidateParentPathSegments = candidateParentPath.Segments.Select(_ => _.Value).ToArray();
        if (candidateParentPathSegments.Length > pathSegments.Length)
        {
            return false;
        }

        return candidateParentPathSegments
            .Select((segment, index) => segment.Equals(pathSegments[index], StringComparison.OrdinalIgnoreCase))
            .All(_ => _);
    }

    /// <summary>
    /// Create the difference that replaces an ancestor difference once a later difference on or below its path is merged into it.
    /// </summary>
    /// <param name="ancestor">The difference already retained.</param>
    /// <param name="descendant">The later difference on or below the ancestor's path.</param>
    /// <param name="state">The state the later difference was recorded with.</param>
    /// <returns>The merged difference.</returns>
    internal static PropertyDifference CreateUpdatedDifferenceFor(PropertyDifference ancestor, PropertyDifference descendant, object state)
    {
        if (ancestor.PropertyPath == descendant.PropertyPath)
        {
            return descendant.IsRemoval
                ? PropertyDifference.Removal(ancestor.PropertyPath, ancestor.Original, ancestor.ArrayIndexers)
                : new PropertyDifference(ancestor.PropertyPath, ancestor.Original, descendant.Changed, ancestor.ArrayIndexers);
        }

        if (!ChangesetStateNavigator.TryGetValueAtPath(state, ancestor.PropertyPath, ancestor.ArrayIndexers, out var changed))
        {
            if (ancestor.IsRemoval)
            {
                return ancestor;
            }

            changed = ancestor.Changed;
        }

        return new PropertyDifference(ancestor.PropertyPath, ancestor.Original, changed, ancestor.ArrayIndexers);
    }
}
