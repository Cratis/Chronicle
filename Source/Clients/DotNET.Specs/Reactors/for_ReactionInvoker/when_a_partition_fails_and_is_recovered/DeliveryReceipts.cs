// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_a_partition_fails_and_is_recovered;

/// <summary>
/// The consumer-owned record of which deliveries have already had their side effect performed. Chronicle does not
/// keep this - it only hands out the identity - so the spec keeps it exactly where a real integration would: on the
/// consumer's side, outliving the reactor instance that wrote it.
/// </summary>
public class DeliveryReceipts
{
    readonly HashSet<DeliveryId> _completed = [];

    public bool HasCompleted(DeliveryId delivery) => _completed.Contains(delivery);

    public void Complete(DeliveryId delivery) => _completed.Add(delivery);
}
