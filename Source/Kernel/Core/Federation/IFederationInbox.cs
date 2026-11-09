// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The local inbox a binding's events are forwarded into: the inbox event sequence of the logical source in the target event store and namespace.
/// </summary>
public interface IFederationInbox
{
    /// <summary>
    /// Find where to resume from the tail of the inbox.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <returns>The resume point, or null when nothing was forwarded yet.</returns>
    Task<FederationResumePoint?> GetResumePoint(FederationBindingState binding);

    /// <summary>
    /// Append a forwarded event to the inbox, recording the origin and source sequence number as causation.
    /// </summary>
    /// <param name="binding">The binding.</param>
    /// <param name="originEventStore">The event store the event originated in.</param>
    /// <param name="event">The event.</param>
    /// <returns>Null on success, otherwise a short, secret free description of the failure.</returns>
    Task<string?> Append(FederationBindingState binding, string originEventStore, FederatedEvent @event);
}
