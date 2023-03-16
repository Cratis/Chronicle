// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Queries;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/failed-partitions")]
public class FailedPartitions : Controller
{
    readonly IFailedPartitionsState _failedPartitionsState;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="failedPartitionsState"><see cref="IFailedPartitionsState"/> for working with the state of observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public FailedPartitions(
        IFailedPartitionsState failedPartitionsState,
        IExecutionContextManager executionContextManager)
    {
        _failedPartitionsState = failedPartitionsState;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Gets all failed partitions.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the failed partitions are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the failed partitions are for.</param>
    /// <returns>Client observable of a collection of <see cref="RecoverFailedPartitionState"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<RecoverFailedPartitionState>>> AllFailedPartitions(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<RecoverFailedPartitionState>>();
        var observable = _failedPartitionsState.All;
        var subscription = observable.Subscribe(_ => clientObservable.OnNext(_));
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
