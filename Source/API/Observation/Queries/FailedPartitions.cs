// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Observation;
using Cratis.Kernel.Storage;
using Cratis.Observation;
using Cratis.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Observation.Queries;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{eventStore}/{namespace}/failed-partitions")]
public class FailedPartitions(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Gets all failed partitions for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the failed partitions are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the failed partitions are for.</param>
    /// <param name="observerId">Optional <see cref="ObserverId"/> to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartitions"/>.</returns>
    [HttpGet("{observerId}")]
    public Task<ClientObservable<IEnumerable<FailedPartition>>> AllFailedPartitions(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace,
        [FromRoute] ObserverId? observerId = default)
    {
        observerId ??= ObserverId.Unspecified;

        var clientObservable = new ClientObservable<IEnumerable<FailedPartition>>();
        var failedPartitions = storage.GetEventStore(eventStore).GetNamespace(@namespace).FailedPartitions;
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
