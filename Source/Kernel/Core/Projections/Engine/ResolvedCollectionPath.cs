// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Places a child collection beneath the collection its parent node actually lives in when the parent type is recursive.
/// </summary>
/// <remarks>
/// A projection definition for a recursive type is finite: <c language="csharp">features.slices</c> is declared once, while a feature can
/// live in <c language="csharp">features</c> or, at any depth, in <c language="csharp">features.subFeatures</c>. Nested nodes of the recursive collection are
/// addressed by repeating its indexer, so a slice of a sub-feature lives at <c language="csharp">features.subFeatures.slices</c> whatever
/// the sub-feature's depth. The child's declared mappings are reused at that location.
/// </remarks>
internal static class ResolvedCollectionPath
{
    /// <summary>
    /// Gets the collection path a child of a resolved parent node is placed in.
    /// </summary>
    /// <param name="projection">The child <see cref="IProjection"/>.</param>
    /// <param name="resolvedParent">The resolved <see cref="Key"/> of the parent node.</param>
    /// <returns>The declared children path, or the path beneath the recursive collection holding the parent node.</returns>
    internal static PropertyPath ForParent(IProjection projection, Key resolvedParent)
    {
        var children = projection.ChildrenPropertyPath;
        var parentLocation = resolvedParent.ArrayIndexers.All.LastOrDefault()?.ArrayProperty;
        if (!projection.HasParent)
        {
            return children;
        }
        if (parentLocation is null)
        {
            return children;
        }
        if (parentLocation == children)
        {
            return children;
        }

        var parent = projection.Parent!;
        var isRecursiveLocation = RecursiveChildProjections.Of(parent).Any(child => child.ChildrenPropertyPath == parentLocation);
        return isRecursiveLocation ? ReplacePrefix(children, parent.ChildrenPropertyPath, parentLocation) : children;
    }

    /// <summary>
    /// Gets the collection path a resolved child key places the child in.
    /// </summary>
    /// <param name="children">The declared children path.</param>
    /// <param name="indexers">The <see cref="ArrayIndexers"/> of the resolved child key.</param>
    /// <returns>The declared path, or the rebased path when the key places the child beneath a recursive collection.</returns>
    internal static PropertyPath ForChild(PropertyPath children, ArrayIndexers indexers)
    {
        if (!children.IsSet)
        {
            return children;
        }
        if (children.IsRoot)
        {
            return children;
        }
        if (indexers.HasFor(children))
        {
            return children;
        }

        var location = indexers.All.LastOrDefault()?.ArrayProperty;
        if (location?.LastSegment.Equals(children.LastSegment) != true)
        {
            return children;
        }

        var ancestors = children.Segments.SkipLast(1).ToArray();
        return location.Segments.Take(ancestors.Length).SequenceEqual(ancestors) ? location : children;
    }

    /// <summary>
    /// Creates a property mapper that writes to the rebased property path when the child is placed beneath a recursive collection.
    /// </summary>
    /// <param name="projection">The <see cref="IProjection"/> the property belongs to.</param>
    /// <param name="property">The declared <see cref="PropertyPath"/>.</param>
    /// <param name="create">Creates a mapper for a given <see cref="PropertyPath"/>.</param>
    /// <returns>The mapper.</returns>
    internal static PropertyMapper<AppendedEvent, ExpandoObject> Mapper(
        IProjection projection,
        PropertyPath property,
        Func<PropertyPath, PropertyMapper<AppendedEvent, ExpandoObject>> create)
    {
        var original = create(property);
        if (!projection.HasParent || !RecursiveChildProjections.Of(projection.Parent!).Any())
        {
            return original;
        }

        var children = projection.ChildrenPropertyPath;
        var rebased = new ConcurrentDictionary<PropertyPath, PropertyMapper<AppendedEvent, ExpandoObject>>();
        return (@event, target, indexers) =>
        {
            var actual = ReplacePrefix(property, children, ForChild(children, indexers));
            var mapper = actual == property ? original : rebased.GetOrAdd(actual, create);
            return mapper(@event, target, indexers);
        };
    }

    static PropertyPath ReplacePrefix(PropertyPath path, PropertyPath prefix, PropertyPath replacement)
    {
        if (prefix == replacement)
        {
            return path;
        }

        var segments = path.Segments.ToArray();
        var prefixSegments = prefix.Segments.ToArray();
        if (!segments.Take(prefixSegments.Length).SequenceEqual(prefixSegments))
        {
            return path;
        }

        var rebasedSegments = replacement.Segments.Concat(segments.Skip(prefixSegments.Length));
        return new PropertyPath(string.Join('.', rebasedSegments.Select(segment => segment.ToString())));
    }
}
