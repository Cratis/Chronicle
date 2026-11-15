// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents an edge of the graph a preview built.
/// </summary>
/// <param name="Type">The type of the edge.</param>
/// <param name="SourceLabel">The label of the source node.</param>
/// <param name="SourceKey">The key of the source node, as text.</param>
/// <param name="TargetLabel">The label of the target node.</param>
/// <param name="TargetKey">The key of the target node, as text.</param>
/// <param name="Identity">The identity of the edge, as text, when the edge type is identified.</param>
/// <param name="Properties">The properties of the edge.</param>
public record GraphPreviewEdge(string Type, string SourceLabel, string SourceKey, string TargetLabel, string TargetKey, string? Identity, JsonObject Properties);
