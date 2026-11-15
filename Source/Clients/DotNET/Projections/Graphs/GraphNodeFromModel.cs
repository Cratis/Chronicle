// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents how an event creates or updates a node.
/// </summary>
/// <param name="EventType">The CLR type of the event.</param>
/// <param name="Key">The key expression, or null to use the event source id.</param>
/// <param name="Properties">The explicit property expressions per property name. Properties not mentioned are mapped by name when the event has them.</param>
/// <param name="UpdateOnly">Whether the event only sets properties on a node that exists.</param>
internal record GraphNodeFromModel(Type EventType, string? Key, IReadOnlyDictionary<string, string> Properties, bool UpdateOnly = false);
