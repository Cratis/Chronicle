// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The node type.</param>
/// <param name="keys">The number of members with the key attribute.</param>
public class NodeMustHaveExactlyOneKey(Type type, int keys) : Exception($"The node type '{type.FullName}' must have exactly one member with the key attribute, but it has {keys}.");
