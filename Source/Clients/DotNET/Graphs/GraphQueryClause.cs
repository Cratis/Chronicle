// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a clause of a graph query. Clauses select roots; they never narrow what a relation holds.
/// </summary>
public abstract record GraphQueryClause;
