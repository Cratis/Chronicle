// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Subscribes pattern capture when a namespace is added after event type registration.
/// </summary>
/// <param name="patternCapture">The pattern capture subscriptions.</param>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription(WellKnownBroadcastChannelNames.NamespaceAdded)]
public class PatternCaptureSubscriptions(
    IPatternCapture patternCapture,
    ILogger<PatternCaptureSubscriptions> logger) : Grain, IPatternCaptureSubscriptions, IOnBroadcastChannelSubscribed
{
    readonly HashSet<NamespaceAdded> _pending = [];

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded);
        return Task.CompletedTask;
    }

    Task OnNamespaceAdded(NamespaceAdded added)
    {
        if (!_pending.Add(added))
        {
            return Task.CompletedTask;
        }

        IGrainTimer? timer = null;
        timer = this.RegisterGrainTimer(
            async _ =>
            {
                timer?.Dispose();
                try
                {
                    await patternCapture.Subscribe(added.EventStore, added.Namespace);
                }
                catch (Exception exception)
                {
                    // The event log reconciles missed or incomplete subscriptions on its next watchdog tick.
                    logger.FailedSubscribing(exception, added.EventStore, added.Namespace);
                }
                finally
                {
                    _pending.Remove(added);
                }
            },

            // The channel is shared by every namespace in the store. Do not hold its broadcast turn (or
            // other namespace timers) through a slow subscription. Duplicates coalesce until it completes.
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, Interleave = true });
        return Task.CompletedTask;
    }
}
