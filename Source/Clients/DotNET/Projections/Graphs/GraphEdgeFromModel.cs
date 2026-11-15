// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents how an event creates or updates an edge.
/// </summary>
/// <param name="EventType">The CLR type of the event.</param>
/// <param name="SourceKey">The source key expression, or null to use the event source id.</param>
/// <param name="TargetKey">The target key expression, or null to use the event source id.</param>
/// <param name="Identity">The identity expression, if the edge has an identifying property.</param>
/// <param name="Properties">The explicit property expressions per property name.</param>
internal record GraphEdgeFromModel(Type EventType, string? SourceKey, string? TargetKey, string? Identity, IReadOnlyDictionary<string, string> Properties);
