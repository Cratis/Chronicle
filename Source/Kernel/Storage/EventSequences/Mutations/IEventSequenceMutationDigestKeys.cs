// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Defines the source of the per-event-store keys that keyed mutation digests are computed with.
/// </summary>
public interface IEventSequenceMutationDigestKeys
{
    /// <summary>
    /// Try to get the digest key of an event store, creating its secret on first use.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <returns>The key, or <see langword="null"/> when it cannot be read or created. Callers fail closed.</returns>
    Task<EventSequenceMutationDigestKey?> TryGetFor(EventStoreName eventStore);
}
