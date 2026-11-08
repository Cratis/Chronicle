// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// A page of public events pulled by a peer from this kernel's outbox.
/// </summary>
/// <param name="Status">One of <see cref="FederatedEventBatchStatus"/>.</param>
/// <param name="OriginEventStore">The event store the events originate in.</param>
/// <param name="Events">The events, in sequence order, already filtered to what the peer may receive.</param>
/// <param name="NextSequenceNumber">The sequence number to pull from next. Advances past events filtered out.</param>
/// <param name="TailSequenceNumber">The current tail of the outbox, so the consumer can compute lag.</param>
[ReadModel]
[AllowAnonymous]
[BelongsTo(WellKnownServices.FederationPull)]
public record FederatedEventBatch(
    string Status,
    string OriginEventStore,
    IEnumerable<FederatedEvent> Events,
    ulong NextSequenceNumber,
    ulong TailSequenceNumber)
{
    /// <summary>
    /// Pulls the next page of events. The caller is authenticated by its pinned client certificate and never by the request.
    /// </summary>
    /// <param name="eventStore">The producer event store.</param>
    /// <param name="namespace">The producer namespace.</param>
    /// <param name="fromSequenceNumber">The first sequence number to consider.</param>
    /// <param name="limit">The maximum number of events to return.</param>
    /// <param name="eventTypes">The event types the consumer wants, intersected with the grant. Empty means everything granted.</param>
    /// <param name="pullService">The <see cref="IFederationPullService"/> that does the work.</param>
    /// <returns>The batch.</returns>
    /// <remarks>
    /// Anonymous with respect to OAuth: a peer presents a pinned client certificate, never a token, and that is decided in the
    /// pull service. This is safe only because <see cref="FederationPortPolicy"/> serves this query on the federation port alone.
    /// </remarks>
    public static Task<FederatedEventBatch> Pull(string eventStore, string @namespace, ulong fromSequenceNumber, int limit, IEnumerable<string> eventTypes, IFederationPullService pullService) =>
        pullService.Pull(eventStore, @namespace, fromSequenceNumber, limit, eventTypes);
}
