// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Captures;

/// <summary>
/// Subscribes the started events captures of an event store in a namespace that is added after they started.
/// </summary>
/// <param name="logger">The logger.</param>
[ImplicitChannelSubscription(WellKnownBroadcastChannelNames.NamespaceAdded)]
public class CaptureEventsNamespaceSubscriptions(ILogger<CaptureEventsNamespaceSubscriptions> logger) : Grain, ICaptureEventsNamespaceSubscriptions, IOnBroadcastChannelSubscribed
{
    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(OnNamespaceAdded);
        return Task.CompletedTask;
    }

    async Task OnNamespaceAdded(NamespaceAdded added)
    {
        try
        {
            await GrainFactory.GetGrain<ICapturesManager>(added.EventStore.Value).NamespaceAdded(added.Namespace);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The captures manager reconciles missing subscriptions on its own timer.
            logger.FailedSubscribingCapturesInNamespace(exception, added.EventStore, added.Namespace);
        }
    }
}
