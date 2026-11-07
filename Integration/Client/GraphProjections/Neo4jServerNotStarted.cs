// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.GraphProjections;

/// <summary>
/// The exception that is thrown when the Neo4j server of the graph projection specifications is asked for before it has started.
/// </summary>
public class Neo4jServerNotStarted() : Exception("The Neo4j server of the graph projection specifications has not started.");
