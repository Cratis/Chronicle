// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Extension methods for working with collection paths in changes.
/// </summary>
public static class ChangeCollectionPathExtensions
{
    /// <summary>
    /// Gets the normalized collection paths affected by child operations.
    /// </summary>
    /// <param name="changes">The changes to inspect.</param>
    /// <returns>A set of normalized collection paths.</returns>
    public static ISet<PropertyPath> GetCollectionPathsWithChildOperations(this IEnumerable<Change> changes)
    {
        var collectionPaths = new HashSet<PropertyPath>();

        foreach (var change in changes)
        {
            switch (change)
            {
                case ChildAdded childAdded:
                    collectionPaths.Add(childAdded.ChildrenProperty.NormalizeCollectionPath());
                    break;

                case ChildRemoved childRemoved:
                    collectionPaths.Add(childRemoved.ChildrenProperty.NormalizeCollectionPath());
                    break;

                case ChildRemovedFromAll childRemovedFromAll:
                    collectionPaths.Add(childRemovedFromAll.ChildrenProperty.NormalizeCollectionPath());
                    break;
            }
        }

        return collectionPaths;
    }

    /// <summary>
    /// Check whether a property difference targets a collection that also has child operations.
    /// </summary>
    /// <param name="difference">The property difference.</param>
    /// <param name="collectionPathsWithChildOperations">Collection paths with child operations.</param>
    /// <returns>True if the property difference conflicts with a child operation; false otherwise.</returns>
    public static bool ConflictsWithChildOperation(this PropertyDifference difference, ISet<PropertyPath> collectionPathsWithChildOperations) =>
        collectionPathsWithChildOperations.Contains(difference.PropertyPath.NormalizeCollectionPath());

    /// <summary>
    /// Creates a state object from differences that do not conflict with child operations.
    /// </summary>
    /// <param name="propertiesChanged">The property change to create state from.</param>
    /// <param name="collectionPathsWithChildOperations">Collection paths with child operations.</param>
    /// <returns>A state object containing only non-conflicting changed properties.</returns>
    public static ExpandoObject ToStateWithoutChildOperationConflicts(this PropertiesChanged<ExpandoObject> propertiesChanged, ISet<PropertyPath> collectionPathsWithChildOperations)
    {
        var state = new ExpandoObject();

        foreach (var difference in propertiesChanged.Differences.Where(_ => !_.ConflictsWithChildOperation(collectionPathsWithChildOperations)))
        {
            difference.PropertyPath.SetValue(state, difference.Changed!, difference.ArrayIndexers);
        }

        return state;
    }

    /// <summary>
    /// Normalize a collection path so array and property segments compare equally.
    /// </summary>
    /// <param name="propertyPath">The property path to normalize.</param>
    /// <returns>The normalized property path.</returns>
    public static PropertyPath NormalizeCollectionPath(this PropertyPath propertyPath)
    {
        var segments = propertyPath.Segments.Select(_ => _.Value);
        return new PropertyPath(string.Join('.', segments));
    }
}
