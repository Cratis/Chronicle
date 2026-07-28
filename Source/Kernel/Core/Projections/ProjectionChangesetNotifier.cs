// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionChangesetNotifier"/>.
/// </summary>
/// <param name="logger">The <see cref="ILogger{T}"/> for logging.</param>
/// <remarks>
/// Marked <c>[KeepAlive]</c> because the subscriber registrations live only in memory — there is no
/// persisted state and no re-subscribe recovery, so a deactivation between a client's
/// <see cref="Subscribe"/> and the projection's <see cref="Notify"/> would drop the registration and the
/// appended event's changeset would never reach the watching client. Every sibling subscription/queue
/// grain (<c>Observer</c>, <c>AppendedEventsQueue</c>) is already <c>[KeepAlive]</c> for the same reason;
/// KeepAlive grains also survive <c>ForceActivationCollection</c>. Fan-out is to
/// <see cref="IReadModelChangesetSubscriber"/> grain references, which Orleans routes reliably to the silo
/// hosting each watch connection.
/// </remarks>
[KeepAlive]
public class ProjectionChangesetNotifier(ILogger<ProjectionChangesetNotifier> logger) : Grain, IProjectionChangesetNotifier
{
    readonly HashSet<IReadModelChangesetSubscriber> _subscribers = [];

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        logger.Activated(this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // A non-zero subscriber count here means in-flight Watch subscriptions are being torn down
        // with the registration — the changeset-delivery-loss failure mode this grain guards against.
        logger.Deactivated(this.GetPrimaryKeyString(), _subscribers.Count, reason.ReasonCode);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Subscribe(IReadModelChangesetSubscriber subscriber)
    {
        _subscribers.Add(subscriber);
        logger.Subscribed(this.GetPrimaryKeyString(), _subscribers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(IReadModelChangesetSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Notify(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change)
    {
        // The subscriber count is the decisive signal: zero here while a client is watching means
        // the registration was lost (deactivation/wiring) rather than the downstream push dropping it.
        logger.Notifying(this.GetPrimaryKeyString(), _subscribers.Count, namespaceName, readModelKey);

        // OnChangeset is [OneWay], so these calls return as soon as the message is sent; the notifier
        // never blocks its grain turn on the downstream push to a client stream.
        await Task.WhenAll(_subscribers.Select(subscriber => subscriber.OnChangeset(namespaceName, readModelKey, readModel, change)));
    }
}
