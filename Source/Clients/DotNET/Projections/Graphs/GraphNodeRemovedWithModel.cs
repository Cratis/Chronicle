// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents how an event deletes a node.
/// </summary>
/// <param name="EventType">The CLR type of the event.</param>
/// <param name="Key">The key expression, or null to use the event source id.</param>
/// <param name="Detach">Whether the edges of the node are deleted with it.</param>
internal record GraphNodeRemovedWithModel(Type EventType, string? Key, bool Detach);
