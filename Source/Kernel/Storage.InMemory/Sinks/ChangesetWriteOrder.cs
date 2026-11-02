// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Decides whether a changeset has to be evaluated as ordered segments, so that the value of the change that comes later in
/// the changeset is the one the row ends with.
/// </summary>
/// <remarks>
/// Root joins are evaluated before the direct changes of the changeset. When a direct change that precedes a join sets the
/// same property as the join, evaluating the join first lets the earlier direct value win. A segment is a join followed by
/// the direct changes after it, so the join-first order is correct inside every segment.
/// </remarks>
internal static class ChangesetWriteOrder
{
    /// <summary>
    /// Splits the changes into segments that, evaluated one after the other, honor the order of the changeset.
    /// </summary>
    /// <param name="changes">The changes of the changeset.</param>
    /// <returns>
    /// The segments, or null when the single pass already honors the order or the changeset is not one this ordering covers.
    /// </returns>
    public static IReadOnlyList<IReadOnlyList<Change>>? SegmentsFor(IReadOnlyList<Change> changes)
    {
        if (!changes.Any(change => change is Joined { HasKeyedFrom: true }) || !IsCovered(changes) || !HasDirectChangeOverlappingALaterJoin(changes))
        {
            return null;
        }

        var segments = new List<IReadOnlyList<Change>>();
        var current = new List<Change>();
        foreach (var change in changes)
        {
            if (change is Joined && current.Count > 0)
            {
                segments.Add(current);
                current = [];
            }

            current.Add(change);
        }

        segments.Add(current);
        return segments;
    }

    static bool IsCovered(IEnumerable<Change> changes) => changes.All(change => change switch
    {
        PropertiesChanged<ExpandoObject> => true,
        ResolvedJoin resolved => IsCovered(resolved.Changes),
        Joined joined => joined.ArrayIndexers.IsEmpty && joined.Changes.All(nested => nested is PropertiesChanged<ExpandoObject>),
        _ => false
    });

    static bool HasDirectChangeOverlappingALaterJoin(IEnumerable<Change> changes)
    {
        var directPaths = new List<string>();
        foreach (var change in changes)
        {
            if (change is Joined joined)
            {
                if (DifferencePaths(joined.Changes).Any(joinedPath => directPaths.Exists(directPath => Overlaps(directPath, joinedPath))))
                {
                    return true;
                }
            }
            else
            {
                directPaths.AddRange(DifferencePaths([change]));
            }
        }

        return false;
    }

    static IEnumerable<string> DifferencePaths(IEnumerable<Change> changes) => changes.SelectMany(change => change switch
    {
        PropertiesChanged<ExpandoObject> propertiesChanged => propertiesChanged.Differences.Select(difference => difference.PropertyPath.Path),
        ResolvedJoin resolved => DifferencePaths(resolved.Changes),
        _ => []
    });

    static bool Overlaps(string first, string second) =>
        first == second || IsAncestor(first, second) || IsAncestor(second, first);

    static bool IsAncestor(string ancestor, string descendant) =>
        descendant.Length > ancestor.Length && descendant.StartsWith(ancestor, StringComparison.Ordinal) && descendant[ancestor.Length] == '.';
}
