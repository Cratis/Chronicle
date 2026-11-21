// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Folds a <see cref="Joined"/> change into the <see cref="ChildAdded"/> of the child it targets, in the same changeset.
/// </summary>
internal static class JoinedChangeNormalizer
{
    /// <summary>
    /// Merge every <see cref="Joined"/> that targets a child added in the same set of changes into that child, and drop it.
    /// </summary>
    /// <param name="changes">The changes to normalize.</param>
    /// <returns>The changes without the merged <see cref="Joined"/> changes.</returns>
    public static IEnumerable<Change> Normalize(IEnumerable<Change> changes)
    {
        var normalizedChanges = changes.ToList();
        var joinedChangesToSkip = new HashSet<Joined>();

        foreach (var joined in normalizedChanges.OfType<Joined>())
        {
            if (!TryMergeIntoChildAdded(normalizedChanges, joined))
            {
                continue;
            }

            joinedChangesToSkip.Add(joined);
        }

        return normalizedChanges.Where(change => change is not Joined joined || !joinedChangesToSkip.Contains(joined));
    }

    static bool TryMergeIntoChildAdded(IList<Change> changes, Joined joined)
    {
        if (joined.ArrayIndexers.IsEmpty)
        {
            return false;
        }

        var targetIndexer = joined.ArrayIndexers.All.Last();
        var matchingChild = changes
            .OfType<ChildAdded>()
            .FirstOrDefault(_ =>
                _.ChildrenProperty == targetIndexer.ArrayProperty &&
                _.IdentifiedByProperty == targetIndexer.IdentifierProperty &&
                _.Key.ToString() == joined.Key.ToString());

        if (matchingChild?.Child is not ExpandoObject child)
        {
            return false;
        }

        ApplyChangesToChild(child, joined.Changes);
        return true;
    }

    static void ApplyChangesToChild(ExpandoObject child, IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    foreach (var difference in propertiesChanged.Differences.Where(_ => !_.PropertyPath.IsMongoDBKey() && !_.PropertyPath.IsSinkOwnedSystemProperty()))
                    {
                        difference.ApplyTo(child, ArrayIndexers.NoIndexers);
                    }

                    break;

                case ResolvedJoin resolvedJoin:
                    ApplyChangesToChild(child, resolvedJoin.Changes);
                    break;
            }
        }
    }
}
