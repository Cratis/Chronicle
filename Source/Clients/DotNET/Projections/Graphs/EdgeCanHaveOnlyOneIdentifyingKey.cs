// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The edge type.</param>
public class EdgeCanHaveOnlyOneIdentifyingKey(Type type) : Exception($"The edge type '{type.FullName}' can have at most one member with the key attribute, which identifies the edge among edges of the same type between the same two nodes.");
