// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents how new children are ordered within a children collection.
/// </summary>
/// <remarks>
/// The ordering is applied when a child is inserted. A child that already exists is updated where it is and never moves,
/// even when the value it is sorted by changes - the order is stored, not computed when read.
/// </remarks>
/// <param name="Placement">The <see cref="ChildrenPlacement"/> for new children.</param>
/// <param name="SortBy">The child property to sort by, used when <paramref name="Placement"/> is <see cref="ChildrenPlacement.Sorted"/>.</param>
/// <param name="Direction">The <see cref="SortDirection"/>, used when <paramref name="Placement"/> is <see cref="ChildrenPlacement.Sorted"/>.</param>
public record ChildrenOrdering(ChildrenPlacement Placement, PropertyPath SortBy, SortDirection Direction = SortDirection.Ascending)
{
    /// <summary>
    /// The default ordering - new children are appended.
    /// </summary>
    public static readonly ChildrenOrdering Append = new(ChildrenPlacement.Append, PropertyPath.NotSet);

    /// <summary>
    /// Check whether the ordering behaves as the default - new children are appended.
    /// </summary>
    /// <remarks>
    /// A sorted ordering without a property to sort by has nothing to sort on, and appends.
    /// </remarks>
    /// <returns>True if new children are appended, false if not.</returns>
    public bool IsAppend() => Placement == ChildrenPlacement.Append || (Placement == ChildrenPlacement.Sorted && !SortBy.IsSet);
}
