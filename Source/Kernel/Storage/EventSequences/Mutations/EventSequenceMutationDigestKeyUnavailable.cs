// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// The exception that is thrown when a keyed mutation digest is computed without the event store's digest key.
/// </summary>
/// <param name="eventStore">The event store whose key is unavailable.</param>
public class EventSequenceMutationDigestKeyUnavailable(EventStoreName eventStore)
    : Exception($"The mutation digest key of event store '{eventStore}' is not available, so its keyed mutation digests cannot be computed.")
{
    /// <summary>
    /// Gets the event store whose key is unavailable.
    /// </summary>
    public EventStoreName EventStore { get; } = eventStore;
}
