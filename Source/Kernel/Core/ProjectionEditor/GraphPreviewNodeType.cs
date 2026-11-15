// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a node label the graph of a preview declares.
/// </summary>
/// <param name="Label">The label.</param>
/// <param name="KeyProperty">The property that holds the key.</param>
/// <param name="Properties">The names of the properties of the node.</param>
public record GraphPreviewNodeType(string Label, string KeyProperty, IEnumerable<string> Properties);
