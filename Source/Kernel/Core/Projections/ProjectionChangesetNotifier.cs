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
/// <param name="projectionsServiceClient">The <see cref="IProjectionsServiceClient"/> used to tell every silo whether the projection is watched.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> used to throttle republishing; defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>
/// One activation exists per observer, event store and namespace (see <see cref="ProjectionChangesetNotifierKey"/>).
/// Marked <c language="csharp">[KeepAlive]</c> because the subscriber registrations live only in memory — there is no
/// persisted state, so each live watch re-registers periodically (<see cref="Subscribe"/> is idempotent), which restores
/// a lost activation. The activation ends when its last watcher unsubscribes. A deactivation between a client's
/// <see cref="Subscribe"/> and the projection's <see cref="Notify"/> would otherwise drop the registration and the
/// appended event's changeset would not reach the watching client until the next re-registration. Every sibling subscription/queue
/// grain (<c language="csharp">Observer</c>, <c language="csharp">AppendedEventsQueue</c>) is already <c language="csharp">[KeepAlive]</c> for the same reason;
/// KeepAlive grains also survive <c language="csharp">ForceActivationCollection</c>. Fan-out is to
/// <see cref="IReadModelChangesetSubscriber"/> grain references, which Orleans routes reliably to the silo
/// hosting each watch connection.
/// </remarks>
[KeepAlive]
public class ProjectionChangesetNotifier(
    ILogger<ProjectionChangesetNotifier> logger,
    IProjectionsServiceClient projectionsServiceClient,
    TimeProvider? timeProvider = null) : Grain, IProjectionChangesetNotifier
{
    static readonly TimeSpan _republishWindow = TimeSpan.FromSeconds(20);

    readonly HashSet<IReadModelChangesetSubscriber> _subscribers = [];
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    IGrainTimer? _publishTimer;
    bool _isPublishing;
    long _lastPublished;
    bool _hasPublished;

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
        var wasUnwatched = _subscribers.Count == 0;
        if (_subscribers.Add(subscriber))
        {
            logger.Subscribed(this.GetPrimaryKeyString(), _subscribers.Count);
        }

        // Becoming watched is always published, never throttled. Otherwise the watch is refreshed by subscribing again,
        // so a subscribe past the republish window renews the watch state.
        if (wasUnwatched || !_hasPublished || _timeProvider.GetElapsedTime(_lastPublished) > _republishWindow)
        {
            SchedulePublish();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(IReadModelChangesetSubscriber subscriber)
    {
        _subscribers.Remove(subscriber);
        if (_subscribers.Count == 0)
        {
            SchedulePublish();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasSubscribers() => Task.FromResult(_subscribers.Count > 0);

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

    /// <inheritdoc/>
    public async Task NotifyMany(EventStoreNamespaceName namespaceName, IReadOnlyList<ReadModelChangeNotification> notifications)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        logger.Notifying(this.GetPrimaryKeyString(), _subscribers.Count, namespaceName, notifications[0].ReadModelKey);
        foreach (var notification in notifications)
        {
            await Task.WhenAll(_subscribers.Select(subscriber => subscriber.OnChangeset(namespaceName, notification.ReadModelKey, notification.ReadModel, notification.Change)));
        }
    }

    void SchedulePublish()
    {
        // A publication in flight reconciles with the current state when it completes, so it needs no second one.
        if (_publishTimer is not null || _isPublishing)
        {
            return;
        }

        // Interleaved one-shot timer: the watch state goes to every silo, which must neither delay Subscribe or Unsubscribe
        // nor hold the turn of this non-reentrant activation while it does.
        _publishTimer = this.RegisterGrainTimer(
            async _ =>
            {
                _publishTimer?.Dispose();
                _publishTimer = null;
                await PublishWatchState();
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, Interleave = true });
    }

    async Task PublishWatchState()
    {
        _isPublishing = true;
        try
        {
            bool isWatched;
            do
            {
                isWatched = _subscribers.Count > 0;
                await TryPublish(isWatched);

                // A subscribe or unsubscribe during the await may have changed the state that was attempted; publish again, whether
                // the attempt succeeded or failed, so a stale state is never the last one attempted. An unchanged state is not retried
                // after a failure: the TTL or the next subscribe renews it.
            }
            while (isWatched != _subscribers.Count > 0);
        }
        finally
        {
            _isPublishing = false;
        }

        if (_subscribers.Count == 0)
        {
            DeactivateOnIdle();
        }
    }

    async Task TryPublish(bool isWatched)
    {
        try
        {
            var key = ProjectionChangesetNotifierKey.Parse(this.GetPrimaryKeyString());
            await projectionsServiceClient.SetWatchState(key.EventStore, key.Namespace, key.ObserverId, isWatched);
            _hasPublished = true;
            _lastPublished = _timeProvider.GetTimestamp();
        }
        catch (Exception ex)
        {
            logger.PublishingWatchStateFailed(ex, this.GetPrimaryKeyString(), isWatched);
        }
    }
}
