// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Extension methods for <see cref="IProjection"/>.
/// </summary>
public static class ProjectionExtensions
{
    /// <summary>
    /// Walks the projection tree and returns every children-collection path the root projection owns.
    /// </summary>
    /// <param name="projection">Root <see cref="IProjection"/> to walk.</param>
    /// <returns>The set of children property paths.</returns>
    /// <remarks>
    /// These paths are owned exclusively by <c>ChildAdded</c> / <c>ChildRemoved</c> across the read model's
    /// lifetime, so nothing may set them from the initial model state - see the pipeline's
    /// <c>NeedsInitialState</c> handling and <c>ChangesetExtensions.AddPropertiesFrom</c>.
    /// <para>
    /// Public rather than private to the pipeline step that first needed it, because anything that reproduces
    /// the initial-state write has to reproduce this exclusion with it. The in-process spec harness is the case
    /// in point: it reimplemented the step without the exclusion, and a spec then saw an empty child collection
    /// as <c>[]</c> where the running system has no such field at all - the one shape no spec could reach.
    /// </para>
    /// </remarks>
    public static IEnumerable<PropertyPath> GetChildrenPropertyPaths(this IProjection projection)
    {
        foreach (var child in projection.ChildProjections)
        {
            if (!child.ChildrenPropertyPath.IsRoot)
            {
                yield return child.ChildrenPropertyPath;
            }

            foreach (var deeper in child.GetChildrenPropertyPaths())
            {
                yield return deeper;
            }
        }
    }
}
