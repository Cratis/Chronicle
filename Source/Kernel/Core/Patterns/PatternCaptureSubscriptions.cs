// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Subscribes pattern capture when a namespace is added after event type registration.
/// </summary>
/// <param name="patternCapture">The pattern capture subscriptions.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">Optional time provider for retry backoff.</param>
[ImplicitChannelSubscription(WellKnownBroadcastChannelNames.NamespaceAdded)]
public class PatternCaptureSubscriptions(
    IPatternCapture patternCapture,
    ILogger<PatternCaptureSubscriptions> logger,
    TimeProvider? timeProvider = null) : Grain, IPatternCaptureSubscriptions, IOnBroadcastChannelSubscribed
{
    const int MaxAttempts = 3;
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded);
        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        var delay = TimeSpan.FromSeconds(1);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await patternCapture.Subscribe(added.EventStore, added.Namespace);
                return;
            }
            catch (Exception exception) when (SiblingSiloInstability.IsTransient(exception) && attempt < MaxAttempts)
            {
                logger.RetryingSubscription(exception, added.EventStore, added.Namespace, attempt, MaxAttempts, delay);
                await Task.Delay(delay, _timeProvider);
                delay += delay;
            }
            catch (Exception exception)
            {
                // NamespaceAdded is fire-and-forget and is not rebroadcast. The event log's reconciliation
                // timer retries independently, even if this notification never reached us at all.
                logger.FailedSubscribing(exception, added.EventStore, added.Namespace);
                return;
            }
        }
    }
}
