// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// Holds well known <see cref="SinkTypeId"/> constants.
/// </summary>
public static class WellKnownSinkTypes
{
    /// <summary>
    /// Gets the identifier of the InMemory projection sink.
    /// </summary>
    public static readonly SinkTypeId InMemory = "InMemory";

    /// <summary>
    /// Gets the identifier of the MongoDB projection sink.
    /// </summary>
    public static readonly SinkTypeId MongoDB = "MongoDB";

    /// <summary>
    /// Gets the identifier of the SQL projection sink.
    /// </summary>
    public static readonly SinkTypeId SQL = "SQL";

    /// <summary>
    /// Gets the identifier of the InMemory graph sink, used for development and tests.
    /// </summary>
    public static readonly SinkTypeId InMemoryGraph = "InMemoryGraph";

    /// <summary>
    /// Gets the identifier of the Neo4j graph sink.
    /// </summary>
    public static readonly SinkTypeId Neo4j = "Neo4j";
}
