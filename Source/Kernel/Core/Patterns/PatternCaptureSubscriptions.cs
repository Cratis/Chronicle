// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Namespaces;
using Orleans.BroadcastChannel;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Subscribes pattern capture when a namespace is added after event type registration.
/// </summary>
/// <param name="patternCapture">The pattern capture subscriptions.</param>
[ImplicitChannelSubscription(WellKnownBroadcastChannelNames.NamespaceAdded)]
public class PatternCaptureSubscriptions(IPatternCapture patternCapture) : Grain, IPatternCaptureSubscriptions, IOnBroadcastChannelSubscribed
{
    /// <inheritdoc/>
    public Task OnSubscribed(IBroadcastChannelSubscription streamSubscription)
    {
        streamSubscription.Attach<NamespaceAdded>(added => patternCapture.Subscribe(added.EventStore, added.Namespace));
        return Task.CompletedTask;
    }
}
