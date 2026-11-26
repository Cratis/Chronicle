// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Security;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a request to preview a graph projection: run the events of the event store through it in memory and read the
/// graph they leave behind, optionally through a view.
/// </summary>
/// <remarks>
/// Nothing is persisted and no graph database is needed. The projection runs against an in-memory graph that is discarded
/// when the request completes.
/// </remarks>
/// <param name="EventStore">The event store the projection targets.</param>
/// <param name="Namespace">The namespace the projection targets.</param>
/// <param name="Declaration">The graph declaration: the graph and the projection ... =&gt; graph that builds it.</param>
/// <param name="Views">The views to declare over the graph; none to preview the nodes and edges only.</param>
/// <param name="View">The name of the view to read; the first of <paramref name="Views"/> when not given.</param>
/// <param name="Clauses">The clauses that select the roots of the view; all of them must hold.</param>
/// <param name="FirstOnly">Whether to read only the first root of the view, in key order.</param>
[StoreAccess(StoreAccess.Read)]
[Command]
public record PreviewGraphProjection(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    string Declaration,
    IEnumerable<GraphViewDeclaration>? Views = null,
    string? View = null,
    IEnumerable<GraphViewClauseDeclaration>? Clauses = null,
    bool FirstOnly = false)
{
    /// <summary>
    /// Handles the preview graph projection request.
    /// </summary>
    /// <param name="previewer">The <see cref="IGraphProjectionPreviewer"/> that runs the preview.</param>
    /// <returns>The graph projection preview.</returns>
    public Task<GraphProjectionPreview> Handle(IGraphProjectionPreviewer previewer) => previewer.Preview(this);
}
