// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Sinks;

/// <summary>
/// Holds well known <see cref="SinkTypeId"/> constants.
/// </summary>
public static class WellKnownSinkTypes
{
    /// <summary>
    /// Gets the event-sequence sink identifier, which publishes state as new events.
    /// Older kernels reject this unknown type rather than ignoring additive target metadata.
    /// </summary>
    public static readonly SinkTypeId EventSequence = "EventSequence";

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
}
