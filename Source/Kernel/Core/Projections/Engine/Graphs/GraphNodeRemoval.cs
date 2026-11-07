// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Represents how an event deletes a node, resolved from a graph projection definition.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Key">The <see cref="ValueProvider{T}"/> for the key of the node.</param>
/// <param name="Detach">Whether the edges attached to the node are deleted with it.</param>
public record GraphNodeRemoval(string Label, ValueProvider<AppendedEvent> Key, bool Detach);
