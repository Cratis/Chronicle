// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Graphs;

/// <summary>
/// Represents the definition of a node label in a graph.
/// </summary>
/// <param name="Label">The node label, for example "Person".</param>
/// <param name="KeyProperty">The name of the property that holds the unique key of a node with this label.</param>
/// <param name="Schema">The <see cref="JsonSchema"/> describing the node properties. Properties are primitives or arrays of primitives only.</param>
public record GraphNodeDefinition(string Label, string KeyProperty, JsonSchema Schema);
