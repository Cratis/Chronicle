// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.Graphs;

/// <summary>
/// Exception that gets thrown when a graph scenario is asked to query a graph it does not run.
/// </summary>
/// <param name="graph">The name of the graph.</param>
public class NoGraphWithName(string graph)
    : Exception($"The scenario runs no graph named '{graph}'. Make sure its node and edge types, or its graph projection, are discoverable by the client artifacts the scenario uses.");
