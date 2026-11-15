// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a view over the graph a graph projection builds, for reading the preview of the graph as nested rows.
/// </summary>
/// <param name="Name">The name of the view, unique among the views of a preview.</param>
/// <param name="Root">The label of the node the view is anchored at.</param>
/// <param name="Members">The members of the view, in the order they appear in a row.</param>
public record GraphViewDeclaration(string Name, string Root, IEnumerable<GraphViewMemberDeclaration> Members);
