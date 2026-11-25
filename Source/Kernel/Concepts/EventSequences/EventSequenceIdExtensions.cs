// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.EventSequences;

/// <summary>
/// Extension methods for <see cref="EventSequenceId"/>.
/// </summary>
public static class EventSequenceIdExtensions
{
    /// <summary>
    /// Determines whether an identifier addresses the system event sequence.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <returns>Whether the trimmed identifier matches the system event sequence.</returns>
    public static bool IsSystemSequence(this EventSequenceId eventSequenceId) =>
        string.Equals(eventSequenceId.Value?.Trim(), EventSequenceId.System.Value, StringComparison.OrdinalIgnoreCase);
}
