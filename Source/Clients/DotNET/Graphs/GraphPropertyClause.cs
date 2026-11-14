// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents equality on a property member of a node.
/// </summary>
/// <param name="Member">The name of the property member.</param>
/// <param name="Property">The node property the member reads.</param>
/// <param name="Value">The value, already converted to what the graph stores (see <see cref="GraphValueConverter"/>).</param>
public sealed record GraphPropertyClause(string Member, string Property, object? Value) : GraphQueryClause;
