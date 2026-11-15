// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a clause that keeps the nodes whose property member equals a value.
/// </summary>
/// <param name="Member">The name of a property member of the view.</param>
/// <param name="Value">The value the property must equal.</param>
public sealed record GraphViewPropertyClause(string Member, object? Value) : GraphViewClause;
