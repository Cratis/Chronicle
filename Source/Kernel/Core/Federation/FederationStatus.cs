// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Represents an implementation of <see cref="IFederationStatus"/> holding thread-safe, in-memory state for this silo.
/// </summary>
/// <param name="timeProvider">Optional <see cref="TimeProvider"/>. Defaults to the system time.</param>
[Singleton]
public class FederationStatus(TimeProvider? timeProvider = null) : IFederationStatus
{
    static readonly FederationLinkStatus _emptyLink = new(null, 0, 0, 0);

    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, FederationLinkStatus> _links = new(StringComparer.Ordinal);
    readonly ConcurrentDictionary<string, FederationBindingStatus> _bindings = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public void RecordPull(string linkId, int events, long bytes)
    {
        var now = _timeProvider.GetUtcNow();
        var deliveredEvents = (ulong)Math.Max(0, events);
        var deliveredBytes = (ulong)Math.Max(0, bytes);
        _links.AddOrUpdate(
            linkId,
            static (_, delivery) => new(delivery.Now, delivery.Events, delivery.Bytes, 0),
            static (_, current, delivery) => current with
            {
                LastPull = delivery.Now,
                EventsDelivered = current.EventsDelivered + delivery.Events,
                BytesDelivered = current.BytesDelivered + delivery.Bytes
            },
            (Now: now, Events: deliveredEvents, Bytes: deliveredBytes));
    }

    /// <inheritdoc/>
    public void RecordThrottled(string linkId) =>
        _links.AddOrUpdate(
            linkId,
            static (_, _) => _emptyLink with { Throttled = 1 },
            static (_, current, _) => current with { Throttled = current.Throttled + 1 },
            0);

    /// <inheritdoc/>
    public FederationLinkStatus GetLink(string linkId) => _links.TryGetValue(linkId, out var status) ? status : _emptyLink;

    /// <inheritdoc/>
    public void SetBinding(string bindingId, FederationBindingStatus status) => _bindings[bindingId] = status;

    /// <inheritdoc/>
    public FederationBindingStatus GetBinding(string bindingId) =>
        _bindings.TryGetValue(bindingId, out var status) ? status : FederationBindingStatus.Unknown;
}
