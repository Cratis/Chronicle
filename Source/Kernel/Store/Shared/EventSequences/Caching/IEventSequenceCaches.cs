// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Defines all the caches for event sequences in the system.
/// </summary>
public interface IEventSequenceCaches
{
    /// <summary>
    /// Get the event sequence cache for a specific key.
    /// </summary>
    /// <param name="key"><see cref="EventSequenceCacheKey"/> to get for.</param>
    /// <returns>The cache.</returns>
    IEventSequenceCache GetFor(EventSequenceCacheKey key);
}
