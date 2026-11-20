// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Kernel.Integration.for_UnseenObserverRemoval.given;

/// <summary>
/// The kernel with the removal on, a sweep every second and dormant release on, as a long running kernel has it.
/// </summary>
public class AutomaticRemovalFixture : UnseenObserverFixture
{
    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, string?> Settings => new Dictionary<string, string?>(CommonSettings)
    {
        ["Observers:UnseenRemovalEnabled"] = "true",
        ["Observers:UnseenRemovalSweepInterval"] = "00:00:01",
        ["Observers:DormantAfter"] = "2",
        ["Observers:DormantSafetyNet"] = "600",
        ["Observers:DurableSubscriptions"] = "Release",
        ["ConnectedClients:SubscriptionCheckIntervalSeconds"] = "4",
        ["ConnectedClients:SubscriptionSettleSeconds"] = "2"
    };
}
