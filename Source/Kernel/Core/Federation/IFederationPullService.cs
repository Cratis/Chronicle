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

    /// <summary>
    /// Read the next page of permitted public events from an outbox, without deciding who may read it. Shared by serving a pull and by a
    /// dialer serving its own outbox: only event types that are public, permitted by a grant and free of protected personal data are returned.
    /// </summary>
    /// <param name="linkId">The link the read is recorded against.</param>
    /// <param name="eventStore">The event store whose outbox is read.</param>
    /// <param name="namespace">The namespace whose outbox is read.</param>
    /// <param name="fromSequenceNumber">The sequence number to read from.</param>
    /// <param name="limit">The most events to return; the configured batch size is the ceiling.</param>
    /// <param name="eventTypes">The event types wanted; empty wants every permitted one.</param>
    /// <param name="permitted">The grants that decide which event types may leave.</param>
    /// <returns>The batch.</returns>
    Task<FederatedEventBatch> Read(
        string linkId,
        string eventStore,
        string @namespace,
        ulong fromSequenceNumber,
        int limit,
        IEnumerable<string> eventTypes,
        IReadOnlyList<FederationGrant> permitted);
}
