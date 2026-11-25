// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents an edge type the graph of a preview declares.
/// </summary>
/// <param name="Type">The type of the edge.</param>
/// <param name="SourceLabel">The label of the source node.</param>
/// <param name="TargetLabel">The label of the target node.</param>
/// <param name="IdentifyingProperty">The property that identifies an edge among edges of the same type between the same two nodes, if the type has one.</param>
/// <param name="Properties">The names of the properties of the edge.</param>
public record GraphPreviewEdgeType(string Type, string SourceLabel, string TargetLabel, string? IdentifyingProperty, IEnumerable<string> Properties);
