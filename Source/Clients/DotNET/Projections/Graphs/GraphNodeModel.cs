// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents a node type with what it is projected from.
/// </summary>
/// <param name="Type">The CLR type of the node.</param>
/// <param name="Label">The node label.</param>
/// <param name="KeyProperty">The name of the key property, as it is named in the graph.</param>
/// <param name="From">The mappings that create or update nodes.</param>
/// <param name="RemovedWith">The mappings that delete nodes.</param>
internal record GraphNodeModel(
    Type Type,
    string Label,
    string KeyProperty,
    IReadOnlyList<GraphNodeFromModel> From,
    IReadOnlyList<GraphNodeRemovedWithModel> RemovedWith);
