// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an edge type with what it is projected from.
/// </summary>
/// <param name="Type">The CLR type of the edge.</param>
/// <param name="EdgeType">The edge type name.</param>
/// <param name="SourceNode">The CLR type of the node the edge starts at.</param>
/// <param name="TargetNode">The CLR type of the node the edge ends at.</param>
/// <param name="IdentifyingProperty">The name of the identifying property, if any.</param>
/// <param name="ExcludedProperties">The names of the members that are not properties of the edge: the ones holding the keys of the end nodes.</param>
/// <param name="From">The mappings that create or update edges.</param>
/// <param name="RemovedWith">The mappings that delete edges.</param>
internal record GraphEdgeModel(
    Type Type,
    string EdgeType,
    Type SourceNode,
    Type TargetNode,
    string? IdentifyingProperty,
    IReadOnlyList<string> ExcludedProperties,
    IReadOnlyList<GraphEdgeFromModel> From,
    IReadOnlyList<GraphEdgeRemovedWithModel> RemovedWith);
