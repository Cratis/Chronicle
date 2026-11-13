// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Decides whether a changeset has to be written as ordered segments, so that the value of the change that comes later in
/// the changeset is the one the document ends with.
/// </summary>
/// <remarks>
/// A root join runs as its own multi-document update before the direct update of the changeset. When a direct change that
/// precedes a join sets the same property as the join, running the join first lets the earlier direct value win. A segment
/// is a join followed by the direct changes after it, so the join-first order of one write is correct inside every segment.
/// </remarks>
internal static class ChangesetWriteOrder
{
    /// <summary>
    /// Splits the changes into segments that, written one after the other, honor the order of the changeset.
    /// </summary>
    /// <param name="changes">The changes of the changeset.</param>
    /// <returns>
    /// The segments, or null when the single pass already honors the order or the changeset is not one this ordering covers.
    /// </returns>
    public static IReadOnlyList<IReadOnlyList<Change>>? SegmentsFor(IEnumerable<Change> changes)
    {
        if (!ContainsKeyedJoin(changes))
        {
            return null;
        }

        var list = changes.ToList();
        if (!NeedsOrdering(list))
        {
            return null;
        }

        var segments = new List<IReadOnlyList<Change>>();
        var current = new List<Change>();
        foreach (var change in list)
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

    static bool NeedsOrdering(List<Change> changes) =>
        IsCovered(changes) && HasDirectChangeOverlappingALaterJoin(changes);

    static bool ContainsKeyedJoin(IEnumerable<Change> changes) => changes switch
    {
        List<Change> list => ContainsKeyedJoin(list),
        IReadOnlyList<Change> readOnlyList => ContainsKeyedJoin(readOnlyList),
        _ => ContainsKeyedJoinInSequence(changes)
    };

    static bool ContainsKeyedJoin(List<Change> changes)
    {
        for (var index = 0; index < changes.Count; index++)
        {
            if (changes[index] is Joined { HasKeyedFrom: true })
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsKeyedJoin(IReadOnlyList<Change> changes)
    {
        for (var index = 0; index < changes.Count; index++)
        {
            if (changes[index] is Joined { HasKeyedFrom: true })
            {
                return true;
            }
        }

        return false;
    }

    static bool ContainsKeyedJoinInSequence(IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            if (change is Joined { HasKeyedFrom: true })
            {
                return true;
            }
        }

        return false;
    }

    static bool IsCovered(IEnumerable<Change> changes) => changes.All(change => change switch
    {
        PropertiesChanged<ExpandoObject> => true,
        ResolvedJoin resolved => IsCovered(resolved.Changes),
        Joined joined => joined.ArrayIndexers.IsEmpty && joined.Changes.All(nested => nested is PropertiesChanged<ExpandoObject>),
        _ => false
    });

    static bool HasDirectChangeOverlappingALaterJoin(IReadOnlyList<Change> changes)
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
