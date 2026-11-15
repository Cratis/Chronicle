// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents how an event deletes an edge.
/// </summary>
/// <param name="EventType">The CLR type of the event.</param>
/// <param name="SourceKey">The source key expression, or null to use the event source id.</param>
/// <param name="TargetKey">The target key expression, or null to use the event source id.</param>
/// <param name="Identity">The identity expression, if the edge has an identifying property.</param>
internal record GraphEdgeRemovedWithModel(Type EventType, string? SourceKey, string? TargetKey, string? Identity);
