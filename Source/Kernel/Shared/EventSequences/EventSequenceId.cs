// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents the unique identifier of an event sequence.
/// </summary>
/// <param name="Value">Actual value.</param>
public record EventSequenceId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The <see cref="EventSequenceId"/> representing an unspecified value.
    /// </summary>
    public static readonly EventSequenceId Unspecified = "[unspecified]";

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the event sequence for the default log.
    /// </summary>
    public static readonly EventSequenceId Log = "event-log";

    /// <summary>
    /// The <see cref="EventSequenceId"/> representing the system event sequence.
    /// </summary>
    public static readonly EventSequenceId System = "system";

    /// <summary>
    /// Get whether or not this is the default log event sequence.
    /// </summary>
    public bool IsEventLog => this == Log;

    /// <summary>
    /// Implicitly convert from a string to <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="id">String to convert from.</param>
    public static implicit operator EventSequenceId(string id) => new(id);
}
