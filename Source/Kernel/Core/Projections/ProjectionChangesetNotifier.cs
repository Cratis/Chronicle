// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionChangesetNotifier"/> using
/// <see cref="ObserverManager{T}"/> to track and dispatch to subscribed observers.
/// </summary>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
/// <remarks>
/// Marked <c>[KeepAlive]</c> because the observer registrations live only in memory (the
/// <see cref="ObserverManager{T}"/> below) — there is no persisted state and no re-subscribe
/// recovery, so a deactivation between a client's <see cref="Subscribe"/> and the projection's
/// <see cref="Notify"/> silently drops the registration and the appended event's changeset never
/// reaches the watching client. Every sibling subscription/queue grain (<c>Observer</c>,
/// <c>AppendedEventsQueue</c>) is already <c>[KeepAlive]</c> for the same reason; KeepAlive grains
/// also survive <c>ForceActivationCollection</c>.
/// </remarks>
[KeepAlive]
public class ProjectionChangesetNotifier(ILogger<ProjectionChangesetNotifier> logger) : Grain, IProjectionChangesetNotifier
{
    /// <summary>
    /// Observer expiration is kept very long because clients legitimately hold long-lived Watch
    /// subscriptions; expiration is driven by explicit Unsubscribe via the gRPC cancellation
    /// token rather than time, and stale observer references that the underlying transport has
    /// already torn down surface as Notify exceptions which ObserverManager handles internally.
    /// </summary>
    readonly ObserverManager<IProjectionChangesetObserver> _observers = new(TimeSpan.FromDays(365 * 4), logger);

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.Activated(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // A non-zero observer count here means in-flight Watch subscriptions are being torn down
        // with the registration — the changeset-delivery-loss failure mode this grain guards against.
        logger.Deactivated(this.GetPrimaryKeyString(), _observers.Count, reason.ReasonCode);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Subscribe(IProjectionChangesetObserver observer)
    {
        _observers.Subscribe(observer, observer);
        logger.Subscribed(this.GetPrimaryKeyString(), _observers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(IProjectionChangesetObserver observer)
    {
        _observers.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel)
    {
        // The observer count is the decisive signal: zero here while a client is watching means
        // the registration was lost (deactivation/wiring) rather than the gRPC push dropping it.
        logger.Notifying(this.GetPrimaryKeyString(), _observers.Count, namespaceName, readModelKey);
        await _observers.Notify(o => o.OnChangeset(namespaceName, readModelKey, readModel));
    }
}
