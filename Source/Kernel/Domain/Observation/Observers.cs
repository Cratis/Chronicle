// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Keys;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Properties;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Domain.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/observers")]
public class Observers : ControllerBase
{
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public Observers(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <summary>
    /// Rewind a specific observer for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/replay/{tenantId}")]
    public async Task Replay(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.Replay();
    }

    /// <summary>
    /// Retry a specific partition for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="partition"><see cref="Key">Partition</see> to retry.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/failed-partitions/{tenantId}/retry/{partition}")]
    public async Task RetryPartition(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId,
        [FromRoute] string partition)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.TryRecoverFailedPartition(new Key(partition, ArrayIndexers.NoIndexers));
    }

    /// <summary>
    /// Rewind a specific observer for a microservice and tenant.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observer is for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observer is for.</param>
    /// <param name="observerId"><see cref="ObserverId"/> to rewind.</param>
    /// <param name="partition">Specific <see cref="Key"/> to rewind.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}/replay/{tenantId}/{partition}")]
    public async Task ReplayPartition(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId observerId,
        [FromRoute] string partition)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerId, new ObserverKey(microserviceId, tenantId, EventSequenceId.Log));
        await observer.ReplayPartition(new Key(partition, ArrayIndexers.NoIndexers));
    }
}
