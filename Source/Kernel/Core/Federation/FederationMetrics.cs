// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The producer-side federation metrics, tagged by link id. Never carries certificate material or event content.
/// </summary>
[Singleton]
public class FederationMetrics
{
    /// <summary>
    /// The name of the meter.
    /// </summary>
    public const string MeterName = "Cratis.Chronicle.Federation";

    /// <summary>
    /// The link tag used when the caller could not be tied to a link.
    /// </summary>
    public const string UnknownLink = "unknown";

    readonly Counter<long> _eventsDelivered;
    readonly Counter<long> _bytesDelivered;
    readonly Counter<long> _throttled;
    readonly Counter<long> _denied;
    readonly Counter<long> _skippedProtected;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationMetrics"/> class.
    /// </summary>
    /// <param name="meterFactory">The <see cref="IMeterFactory"/> to create the meter with.</param>
    public FederationMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable CA2000 // The meter is owned by the IMeterFactory, which disposes it.
        var meter = meterFactory.Create(MeterName);
#pragma warning restore CA2000
        _eventsDelivered = meter.CreateCounter<long>("chronicle.federation.events.delivered", "{event}", "Events delivered to federation peers");
        _bytesDelivered = meter.CreateCounter<long>("chronicle.federation.bytes.delivered", "By", "Event content bytes delivered to federation peers");
        _throttled = meter.CreateCounter<long>("chronicle.federation.pulls.throttled", "{pull}", "Federation pulls refused because the link exceeded its rate limit");
        _denied = meter.CreateCounter<long>("chronicle.federation.pulls.denied", "{pull}", "Federation pulls refused because the caller is not authenticated or not granted what it asked for");
        _skippedProtected = meter.CreateCounter<long>("chronicle.federation.events.skipped.protected", "{event}", "Events never forwarded because their event type carries protected personal data");
    }

    /// <summary>
    /// Record a served pull.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="events">The number of events delivered.</param>
    /// <param name="bytes">The number of content bytes delivered.</param>
    public virtual void Delivered(string linkId, int events, long bytes)
    {
        var tag = Link(linkId);
        _eventsDelivered.Add(events, tag);
        _bytesDelivered.Add(bytes, tag);
    }

    /// <summary>
    /// Record a throttled pull.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    public virtual void Throttled(string linkId) => _throttled.Add(1, Link(linkId));

    /// <summary>
    /// Record a denied pull.
    /// </summary>
    /// <param name="linkId">The link id, or <see cref="UnknownLink"/> when the caller is not authenticated.</param>
    public virtual void Denied(string linkId) => _denied.Add(1, Link(linkId));

    /// <summary>
    /// Record events that were not forwarded because they carry protected data.
    /// </summary>
    /// <param name="linkId">The link id.</param>
    /// <param name="events">The number of events skipped.</param>
    public virtual void SkippedProtected(string linkId, int events) => _skippedProtected.Add(events, Link(linkId));

    static KeyValuePair<string, object?> Link(string linkId) => new("chronicle.federation.link", linkId);
}
