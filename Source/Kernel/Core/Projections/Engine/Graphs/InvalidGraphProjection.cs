// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Exception that gets thrown when a graph projection definition does not fit the graph it targets.
/// </summary>
/// <param name="projection">The identifier of the projection.</param>
/// <param name="reason">Why the definition is invalid.</param>
public class InvalidGraphProjection(string projection, string reason)
    : Exception($"The graph projection '{projection}' is invalid: {reason}");
