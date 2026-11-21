// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a preview of a graph projection: the graph the events left behind in memory, and what a view reads from it.
/// </summary>
/// <param name="Graph">The graph the projection declares; absent when the declaration has syntax errors.</param>
/// <param name="Nodes">The nodes of the graph, by label and key.</param>
/// <param name="Edges">The edges of the graph, by type, source, target and identity.</param>
/// <param name="Rows">The rows a view query read from the graph, with the nested rows of its relations; empty when no view was queried.</param>
/// <param name="EventsProcessed">The number of events the projection ran through.</param>
/// <param name="EventErrors">The events the projection could not apply.</param>
/// <param name="ViewErrors">The rules the views or the query broke.</param>
/// <param name="SyntaxErrors">The syntax errors encountered while compiling the declaration.</param>
public record GraphProjectionPreview(
    GraphPreviewSchema? Graph,
    IEnumerable<GraphPreviewNode> Nodes,
    IEnumerable<GraphPreviewEdge> Edges,
    IEnumerable<JsonObject> Rows,
    int EventsProcessed,
    IEnumerable<GraphPreviewEventError> EventErrors,
    IEnumerable<GraphViewError> ViewErrors,
    IEnumerable<ProjectionDeclarationSyntaxError> SyntaxErrors);
