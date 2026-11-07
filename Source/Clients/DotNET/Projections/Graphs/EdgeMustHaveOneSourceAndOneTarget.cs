// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Exception that gets thrown when a graph declaration is invalid.
/// </summary>
/// <param name="type">The edge type.</param>
public class EdgeMustHaveOneSourceAndOneTarget(Type type) : Exception($"The edge type '{type.FullName}' must have exactly one member marked as source and one marked as target.");
