// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The node or edge type.</param>
/// <param name="property">The name of the property.</param>
public class NestedObjectInGraph(Type type, string property) : Exception($"The property '{property}' of '{type.FullName}' is an object. Nodes and edges can only have primitive properties and arrays of primitives.");
