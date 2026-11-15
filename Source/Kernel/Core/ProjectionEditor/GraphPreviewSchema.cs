// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents the graph a graph projection declares.
/// </summary>
/// <param name="Name">The name of the graph.</param>
/// <param name="NodeTypes">The node labels of the graph.</param>
/// <param name="EdgeTypes">The edge types of the graph.</param>
public record GraphPreviewSchema(string Name, IEnumerable<GraphPreviewNodeType> NodeTypes, IEnumerable<GraphPreviewEdgeType> EdgeTypes);
