// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// Collects the identity of every delivery a reactor has been handed, outliving the reactor instances that record
/// into it the way a consumer's own receipt storage would.
/// </summary>
public class DeliveryLog
{
    readonly List<DeliveryId> _deliveries = [];

    /// <summary>
    /// Gets the identity of every delivery recorded, in order.
    /// </summary>
    public IReadOnlyList<DeliveryId> Deliveries => _deliveries;

    /// <summary>
    /// Records a delivery.
    /// </summary>
    /// <param name="delivery">The <see cref="DeliveryId"/> to record.</param>
    public void Record(DeliveryId delivery) => _deliveries.Add(delivery);
}
