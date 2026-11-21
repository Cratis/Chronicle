// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when the schema-included form of a definition is asked for and the definition has no schema.
/// </summary>
/// <param name="graph">The name of the graph.</param>
public class GraphViewDefinitionHasNoSchema(string graph)
    : Exception($"The view definition of the graph '{graph}' has no schema to write.");
