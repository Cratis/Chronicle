// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Concepts.Graphs;

/// <summary>
/// Represents the definition of an edge type in a graph.
/// </summary>
/// <param name="Type">The edge type, for example "MEMBER_OF".</param>
/// <param name="SourceLabel">The label of the node an edge of this type starts at.</param>
/// <param name="TargetLabel">The label of the node an edge of this type ends at.</param>
/// <param name="IdentifyingProperty">
/// Optional name of a property that identifies an edge among edges of the same type between the same two nodes.
/// When not set an edge is identified by its type, source and target, so there is at most one edge between a pair of nodes.
/// </param>
/// <param name="Schema">The <see cref="JsonSchema"/> describing the edge properties. Properties are primitives or arrays of primitives only.</param>
public record GraphEdgeDefinition(string Type, string SourceLabel, string TargetLabel, string? IdentifyingProperty, JsonSchema Schema);
