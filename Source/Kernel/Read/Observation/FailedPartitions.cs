// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Observation;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/failed-partitions")]
public class FailedPartitions : ControllerBase
{
    readonly IClusterStorage _clusterStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing underlying storage.</param>
    public FailedPartitions(IClusterStorage clusterStorage)
    {
        _clusterStorage = clusterStorage;
    }

    /// <summary>
    /// Gets all failed partitions.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the failed partitions are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the failed partitions are for.</param>
    /// <param name="observerId">Optional <see cref="ObserverId"/> to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartitions"/>.</returns>
    [HttpGet("{observerId}")]
    public Task<ClientObservable<IEnumerable<FailedPartition>>> AllFailedPartitions(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId? observerId = default)
    {
        observerId ??= ObserverId.Unspecified;

        var clientObservable = new ClientObservable<IEnumerable<FailedPartition>>();
        var failedPartitions = _clusterStorage.GetEventStore((string)microserviceId).GetNamespace(tenantId).FailedPartitions;
        var observable = failedPartitions.ObserveAllFor(observerId);
        var subscription = observable.Subscribe(clientObservable.OnNext);
        clientObservable.ClientDisconnected = () =>
        {
            subscription.Dispose();
            if (observable is IDisposable disposableObservable)
            {
                disposableObservable.Dispose();
            }
        };

        return Task.FromResult(clientObservable);
    }
}
