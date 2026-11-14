// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents an edge of a <see cref="GraphViewSchema"/>.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="Source">The label of the node the edge starts at.</param>
/// <param name="Target">The label of the node the edge ends at.</param>
/// <param name="IdentifiedBy">The identifying property, or null when the edge is identified by its type, source and target.</param>
/// <param name="Properties">The names of the properties, in declaration order.</param>
public sealed record GraphViewSchemaEdge(string Type, string Source, string Target, string? IdentifiedBy, IReadOnlyList<string> Properties);
