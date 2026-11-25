// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Describes the search for the parent of a future in the current read model state.
/// </summary>
/// <param name="CurrentState">The root <see cref="ExpandoObject"/> representing the current read model state.</param>
/// <param name="Changeset">The changeset for the current root event, including initialization changes.</param>
/// <param name="RootKey">The resolved key of the root document currently being processed.</param>
/// <param name="ChildProjection">The child projection whose parent chain is being resolved.</param>
/// <param name="ParentPath">The <see cref="PropertyPath"/> identifying the parent's collection.</param>
/// <param name="ParentIdentifiedByProperty">The <see cref="PropertyPath"/> of the property that identifies items in the parent collection.</param>
/// <param name="ParentKey">The key value of the parent item being located.</param>
internal readonly record struct ParentSearch(
    ExpandoObject? CurrentState,
    IChangeset<AppendedEvent, ExpandoObject> Changeset,
    object RootKey,
    IProjection ChildProjection,
    PropertyPath ParentPath,
    PropertyPath ParentIdentifiedByProperty,
    object ParentKey)
{
    /// <summary>
    /// Describe the search for the parent of a future while processing an event.
    /// </summary>
    /// <param name="childProjection">The child projection the future belongs to.</param>
    /// <param name="context">The <see cref="ProjectionEventContext"/> of the event being processed.</param>
    /// <param name="future">The <see cref="ProjectionFuture"/> whose parent is searched for.</param>
    /// <param name="parentKey">The key value of the parent item being located.</param>
    /// <returns>The <see cref="ParentSearch"/>.</returns>
    internal static ParentSearch For(IProjection childProjection, ProjectionEventContext context, ProjectionFuture future, object parentKey) =>
        new(
            context.Changeset.CurrentState,
            context.Changeset,
            context.Key.Value,
            childProjection,
            future.ParentPath,
            future.ParentIdentifiedByProperty,
            parentKey);
}
