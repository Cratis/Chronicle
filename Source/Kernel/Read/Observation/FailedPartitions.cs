// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Observation;

/// <summary>
/// Represents the API for getting failed partitions.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/failed-partitions")]
public class FailedPartitions : ControllerBase
{
    readonly ProviderFor<IFailedPartitionsStorage> _failedPartitionsStateProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="failedPartitionsStateProvider">Provider for <see cref="IFailedPartitionsStorage"/> for working with the state of observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public FailedPartitions(
        ProviderFor<IFailedPartitionsStorage> failedPartitionsStateProvider,
        IExecutionContextManager executionContextManager)
    {
        _failedPartitionsStateProvider = failedPartitionsStateProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Gets all failed partitions.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the failed partitions are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the failed partitions are for.</param>
    /// <param name="observerId">Optional <see cref="ObserverId"/> to filter down which observer it is for.</param>
    /// <returns>Client observable of a collection of <see cref="FailedPartitions"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<FailedPartition>>> AllFailedPartitions(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId,
        [FromRoute] ObserverId? observerId = default)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);

        observerId ??= ObserverId.Unspecified;

        var clientObservable = new ClientObservable<IEnumerable<FailedPartition>>();
        var observable = _failedPartitionsStateProvider().ObserveAllFor(observerId);
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
