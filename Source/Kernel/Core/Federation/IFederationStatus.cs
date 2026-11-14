// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// In-memory, per-silo operational state of the federation links, for read models, metrics and the Workbench.
/// </summary>
public interface IFederationStatus
{
    /// <summary>
    /// Record a pull served to a peer.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="events">The number of events delivered.</param>
    /// <param name="bytes">The number of content bytes delivered.</param>
    void RecordPull(string linkId, int events, long bytes);

    /// <summary>
    /// Record that a pull was throttled.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    void RecordThrottled(string linkId);

    /// <summary>
    /// Get the producer-side state of a link.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <returns>The state, with zeros when nothing has been recorded.</returns>
    FederationLinkStatus GetLink(string linkId);

    /// <summary>
    /// Record events a dialer served to this kernel on the inbound direction of a hosted link.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="events">The number of events appended.</param>
    /// <param name="bytes">The number of content bytes appended.</param>
    void RecordInbound(string linkId, int events, long bytes);

    /// <summary>
    /// Get the inbound state of a hosted link.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <returns>The state, with zeros when nothing has been recorded.</returns>
    FederationInboundStatus GetInbound(string linkId);

    /// <summary>
    /// Record that a dialer session of a subject was opened on this silo.
    /// </summary>
    /// <param name="subject">The subject.</param>
    void RecordSessionOpened(string subject);

    /// <summary>
    /// Record that a dialer session of a subject ended on this silo.
    /// </summary>
    /// <param name="subject">The subject.</param>
    void RecordSessionClosed(string subject);

    /// <summary>
    /// Get whether a dialer session of a subject is open on this silo.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <returns>True when a session is open, otherwise false.</returns>
    bool HasSession(string subject);

    /// <summary>
    /// Record the consumer-side state of a binding.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <param name="status">The state.</param>
    void SetBinding(string bindingId, FederationBindingStatus status);

    /// <summary>
    /// Get the consumer-side state of a binding.
    /// </summary>
    /// <param name="bindingId">The binding id.</param>
    /// <returns>The state, <see cref="FederationBindingStatus.Unknown"/> when nothing has been recorded.</returns>
    FederationBindingStatus GetBinding(string bindingId);
}
