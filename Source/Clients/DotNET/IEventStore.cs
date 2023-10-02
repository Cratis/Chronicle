// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis;

/// <summary>
/// Defines the event store API surface.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets the <see cref="IEventLog"/> event sequence.
    /// </summary>
    IEventLog EventLog { get; }

    /// <summary>
    /// Gets the <see cref="IEventOutbox"/> event sequence.
    /// </summary>
    IEventOutbox EventOutbox { get; }

    /// <summary>
    /// Get an event sequence by id.
    /// </summary>
    /// <param name="id">The identifier of the event sequence to get.</param>
    /// <returns><see cref="IEventSequence"/> instance.</returns>
    IEventSequence GetEventSequence(EventSequenceId id);
}
