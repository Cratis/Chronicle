// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Graphs;

/// <summary>
/// Represents how an event deletes an edge.
/// </summary>
/// <param name="SourceKey"><see cref="PropertyExpression"/> resolving the key of the source node.</param>
/// <param name="TargetKey"><see cref="PropertyExpression"/> resolving the key of the target node.</param>
/// <param name="Identity">Optional <see cref="PropertyExpression"/> resolving the value of the identifying property of the edge.</param>
public record GraphEdgeRemovedWithDefinition(PropertyExpression SourceKey, PropertyExpression TargetKey, PropertyExpression? Identity);
