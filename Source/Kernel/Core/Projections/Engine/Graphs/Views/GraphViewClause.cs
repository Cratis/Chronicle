// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a condition a query puts on the roots of a view. Clauses only select roots; they never narrow a relation.
/// </summary>
public abstract record GraphViewClause;
