// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The type.</param>
public class GraphElementCannotBeReadModel(Type type) : Exception($"The type '{type.FullName}' is a node or an edge in a graph and cannot also be a read model.");
