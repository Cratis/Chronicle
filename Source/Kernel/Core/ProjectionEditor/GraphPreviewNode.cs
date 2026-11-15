// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a node of the graph a preview built.
/// </summary>
/// <param name="Label">The label of the node.</param>
/// <param name="Key">The key of the node, as text.</param>
/// <param name="Properties">The properties of the node.</param>
public record GraphPreviewNode(string Label, string Key, JsonObject Properties);
