// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;
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
    public static bool ConflictsWithChildOperation(this PropertyDifference difference, ISet<PropertyPath> collectionPathsWithChildOperations)
    {
        var normalizedPropertyPath = difference.PropertyPath.NormalizeCollectionPath();

        return collectionPathsWithChildOperations.Any(normalizedPropertyPath.IsSameAsOrDescendantOf);
    }

    /// <summary>
    /// Gets the normalized paths of collections that are replaced as a whole by a single property difference.
    /// </summary>
    /// <param name="changes">The changes to inspect.</param>
    /// <returns>A set of normalized collection paths replaced wholesale.</returns>
    /// <remarks>
    /// A difference whose property path ends in an array segment (e.g. the collapsed compliance difference
    /// for <c>contacts</c>) replaces the entire collection in one <c>$set</c>. Any element-level difference
    /// under that same collection must then be dropped — a sink cannot apply both a whole-collection
    /// replacement and a positional update to the same path in one operation.
    /// </remarks>
    public static ISet<PropertyPath> GetWholeCollectionReplacementPaths(this IEnumerable<Change> changes)
    {
        var paths = new HashSet<PropertyPath>();

        foreach (var propertiesChanged in changes.OfType<PropertiesChanged<ExpandoObject>>())
        {
            foreach (var difference in propertiesChanged.Differences)
            {
                if (difference.PropertyPath.LastSegment is ArrayProperty)
                {
                    paths.Add(difference.PropertyPath.NormalizeCollectionPath());
                }
            }
        }

        return paths;
    }

    /// <summary>
    /// Check whether a property difference targets an element inside a collection that is replaced as a whole.
    /// </summary>
    /// <param name="difference">The property difference.</param>
    /// <param name="wholeCollectionReplacementPaths">Collection paths replaced wholesale.</param>
    /// <returns>True if the property difference is a strict descendant of a wholesale replacement; false otherwise.</returns>
    public static bool ConflictsWithWholeCollectionReplacement(this PropertyDifference difference, ISet<PropertyPath> wholeCollectionReplacementPaths)
    {
        var normalizedPropertyPath = difference.PropertyPath.NormalizeCollectionPath();

        return wholeCollectionReplacementPaths.Any(replacement =>
            normalizedPropertyPath != replacement &&
            replacement.IsSet &&
            normalizedPropertyPath.Path.StartsWith($"{replacement.Path}.", StringComparison.Ordinal));
    }

    /// <summary>
    /// Filters property differences, dropping those that target an element inside a collection that is
    /// concurrently modified by a child operation (push/pull) or replaced wholesale by a single $set.
    /// </summary>
    /// <param name="differences">The property differences to filter.</param>
    /// <param name="collectionPathsWithChildOperations">Collection paths with child operations.</param>
    /// <param name="wholeCollectionReplacementPaths">Optional collection paths replaced wholesale; element-level differences under them are dropped.</param>
    /// <returns>The non-conflicting property differences.</returns>
    /// <remarks>
    /// A sink cannot apply both a collection-level operation and an element-level update to the same path in
    /// one operation — MongoDB rejects it as a path conflict — so the element-level difference is dropped;
    /// the child operation or whole-collection replacement already carries the latest element value.
    /// </remarks>
    public static IEnumerable<PropertyDifference> WithoutCollectionConflicts(
        this IEnumerable<PropertyDifference> differences,
        ISet<PropertyPath> collectionPathsWithChildOperations,
        ISet<PropertyPath>? wholeCollectionReplacementPaths = null) =>
        differences.Where(_ =>
            !_.ConflictsWithChildOperation(collectionPathsWithChildOperations) &&
            (wholeCollectionReplacementPaths is null || !_.ConflictsWithWholeCollectionReplacement(wholeCollectionReplacementPaths)));

    /// <summary>
    /// Applies property differences to an existing state object while excluding properties that conflict with child operations.
    /// </summary>
    /// <param name="propertiesChanged">The property change to apply.</param>
    /// <param name="state">The state to apply the changes to.</param>
    /// <param name="collectionPathsWithChildOperations">Collection paths with child operations.</param>
    /// <param name="wholeCollectionReplacementPaths">Optional collection paths replaced wholesale; element-level differences under them are dropped.</param>
    /// <returns>A state object with non-conflicting property differences applied.</returns>
    public static ExpandoObject ApplyToStateWithoutChildOperationConflicts(this PropertiesChanged<ExpandoObject> propertiesChanged, ExpandoObject state, ISet<PropertyPath> collectionPathsWithChildOperations, ISet<PropertyPath>? wholeCollectionReplacementPaths = null)
    {
        var result = state.Clone();

        foreach (var difference in propertiesChanged.Differences.WithoutCollectionConflicts(collectionPathsWithChildOperations, wholeCollectionReplacementPaths))
        {
            difference.PropertyPath.SetValue(result, difference.Changed!, difference.ArrayIndexers);
        }

        return result;
    }

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

    static bool IsSameAsOrDescendantOf(this PropertyPath propertyPath, PropertyPath candidateParentPath) =>
        propertyPath == candidateParentPath ||
        (candidateParentPath.IsSet && propertyPath.Path.StartsWith($"{candidateParentPath.Path}.", StringComparison.Ordinal));
}
