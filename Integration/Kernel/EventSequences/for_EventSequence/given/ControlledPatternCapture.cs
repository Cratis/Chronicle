// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Patterns;

using KernelEventStoreName = Cratis.Chronicle.Concepts.EventStoreName;
using KernelEventStoreNamespaceName = Cratis.Chronicle.Concepts.EventStoreNamespaceName;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.given;

public class ControlledPatternCapture(PatternCapture inner, PatternCaptureControl control) : IPatternCapture
{
    public Task Subscribe(KernelEventStoreName eventStore, KernelEventStoreNamespaceName @namespace) => inner.Subscribe(eventStore, @namespace);

    public Task SubscribeAcrossNamespaces(KernelEventStoreName eventStore) => inner.SubscribeAcrossNamespaces(eventStore);

    public async Task<bool> EnsureSubscribedForDurableAppend(KernelEventStoreName eventStore, KernelEventStoreNamespaceName @namespace)
    {
        var isTarget = eventStore == control.Key.EventStore && @namespace == control.Key.Namespace;
        if (isTarget)
        {
            control.SubscriptionStarted.TrySetResult();
            if (control.HoldSubscription)
            {
                await control.SubscriptionReleased.Task;
            }
        }

        var subscribed = await inner.EnsureSubscribedForDurableAppend(eventStore, @namespace);
        if (isTarget && subscribed)
        {
            control.SubscriptionCompleted.TrySetResult();
        }

        return subscribed;
    }
}
