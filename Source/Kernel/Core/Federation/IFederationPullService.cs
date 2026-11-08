// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Serves federation pulls: authenticates the calling peer by its pinned client certificate, enforces its grant and
/// rate limit, and reads the permitted public events from the outbox.
/// </summary>
public interface IFederationPullService
{
    /// <summary>
    /// Pull the next page of events for the caller of the current connection.
    /// </summary>
    /// <param name="eventStore">The producer event store.</param>
    /// <param name="namespace">The producer namespace.</param>
    /// <param name="fromSequenceNumber">The first sequence number to consider.</param>
    /// <param name="limit">The maximum number of events to return.</param>
    /// <param name="eventTypes">The event types the consumer wants, intersected with the grant. Empty means everything granted.</param>
    /// <returns>The batch. Denied and throttled batches carry no events.</returns>
    Task<FederatedEventBatch> Pull(string eventStore, string @namespace, ulong fromSequenceNumber, int limit, IEnumerable<string> eventTypes);
}
