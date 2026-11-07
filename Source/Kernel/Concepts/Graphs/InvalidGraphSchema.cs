// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Graphs;

/// <summary>
/// Exception that gets thrown when a schema that does not describe a graph is read as a graph.
/// </summary>
public class InvalidGraphSchema() : Exception("The schema does not describe a graph.");
