// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a member of a <see cref="GraphView"/>.
/// </summary>
/// <param name="Name">The name of the member, which is its name in a result row.</param>
/// <param name="Kind">What the member reads.</param>
/// <param name="Source">The node property or edge property a property member reads.</param>
/// <param name="Edge">The edge type a relation follows.</param>
/// <param name="Direction">The explicit direction of a relation; derived from the root of the view when not given.</param>
/// <param name="Cardinality">The cardinality of a relation.</param>
/// <param name="View">The name of the view a relation leads to.</param>
public record GraphViewMember(
    string Name,
    GraphViewMemberKind Kind,
    string? Source = null,
    string? Edge = null,
    GraphViewDirection? Direction = null,
    GraphViewCardinality? Cardinality = null,
    string? View = null);
