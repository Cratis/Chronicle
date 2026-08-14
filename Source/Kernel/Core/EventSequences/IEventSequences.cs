// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines a system that manages all the event sequences for an event store and namespace.
/// </summary>
public interface IEventSequences : IGrainWithIntegerCompoundKey
{
    /// <summary>
    /// Get the event sequences that exist for the event store and namespace.
    /// </summary>
    /// <returns>A collection of <see cref="EventSequenceId"/>.</returns>
    /// <remarks>
    /// The well-known sequences are always included, whether or not anything has been appended to
    /// them yet - they are what the namespace offers, rather than what it happens to hold.
    /// </remarks>
    Task<IEnumerable<EventSequenceId>> GetEventSequences();

    /// <summary>
    /// Rehydrate the event sequences.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rehydrate();
}
