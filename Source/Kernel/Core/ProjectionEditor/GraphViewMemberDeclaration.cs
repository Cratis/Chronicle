// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProjectionEditor;

/// <summary>
/// Represents a member of a <see cref="GraphViewDeclaration"/>.
/// </summary>
/// <param name="Name">The name of the member, which is its name in a result row.</param>
/// <param name="Kind">What the member reads: property of the root node, relation along an edge to another view, or edgeProperty of the edge that was followed.</param>
/// <param name="Source">The node property or edge property a property or edgeProperty member reads.</param>
/// <param name="Edge">The edge type a relation follows.</param>
/// <param name="Direction">The direction of a relation, outgoing or incoming; derived from the root of the view when not given, and required for an edge between nodes of the same label.</param>
/// <param name="Cardinality">The cardinality of a relation, many (the default) or one.</param>
/// <param name="View">The name of the view a relation leads to.</param>
public record GraphViewMemberDeclaration(
    string Name,
    string Kind,
    string? Source = null,
    string? Edge = null,
    string? Direction = null,
    string? Cardinality = null,
    string? View = null);
