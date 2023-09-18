// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Grains.Observation.New;

/// <summary>
/// Represents an implementation of <see cref="IObserverEventHandler"/>.
/// </summary>
public class ObserverEventHandler : IObserverEventHandler
{
    readonly ObserverId _observerId;
    readonly MicroserviceId _microserviceId;
    readonly TenantId _tenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverEventHandler"/> class.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> the handler is for.</param>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the handler is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the handler is for.</param>
    public ObserverEventHandler(
        ObserverId observerId,
        MicroserviceId microserviceId,
        TenantId tenantId)
    {
        _observerId = observerId;
        _microserviceId = microserviceId;
        _tenantId = tenantId;
    }

    /// <inheritdoc/>
    public Task Handle(AppendedEvent @event)
    {
        // Info it needs:
        // - Current subscription information
        // - Failed Partitions
        // - Current Sequence Number

        // If observer is disconnected, we should not handle the event

        // If sequence number is greater than or equal to next event sequence number, we should not handle the event

        // If Partition is failed, we should not handle the event

        // For replaying or failed partition recovery of a specific partition, the NextSequenceNumber shouldn't be updated.


        return Task.CompletedTask;
    }
}
