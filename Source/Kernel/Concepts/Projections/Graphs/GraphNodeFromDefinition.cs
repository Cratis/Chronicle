// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents how an event creates or updates a node.
/// </summary>
/// <param name="Key"><see cref="PropertyExpression"/> resolving the key of the node from the event.</param>
/// <param name="Properties">Property name to expression, including counter expressions such as "$increment".</param>
/// <param name="UpdateOnly">Whether the event only sets properties on a node that already exists, instead of creating it.</param>
public record GraphNodeFromDefinition(PropertyExpression Key, IDictionary<string, string> Properties, bool UpdateOnly = false);
