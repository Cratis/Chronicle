// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a member of a <see cref="GraphQueryNode"/>.
/// </summary>
/// <param name="Name">The name of the member, the key it has in a <see cref="GraphRow"/>.</param>
/// <param name="Kind">The <see cref="GraphViewMemberKind"/>.</param>
/// <param name="Source">The node or edge property it reads, for a property or an edge property; otherwise null.</param>
/// <param name="Relation">The <see cref="GraphQueryRelation"/>, for a relation; otherwise null.</param>
public sealed record GraphQueryMember(string Name, GraphViewMemberKind Kind, string? Source = null, GraphQueryRelation? Relation = null);
