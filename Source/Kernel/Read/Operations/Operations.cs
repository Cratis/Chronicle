// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Grains.Operations;
using Aksio.Cratis.Kernel.Operations;
using Aksio.Cratis.Kernel.Persistence.Operations;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Operations;

/// <summary>
/// Represents the API for working with operations.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/operations")]
public class Operations : ControllerBase
{
    readonly ProviderFor<IOperationStorage> _operationStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Operations"/> class.
    /// </summary>
    /// <param name="operationStorageProvider">Provider for <see cref="IOperationStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Operations(
        ProviderFor<IOperationStorage> operationStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _operationStorageProvider = operationStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Collection of <see cref="OperationInformation"/>.</returns>
    [HttpGet]
    public async Task<IEnumerable<OperationInformation>> GetOperations(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);
        var operations = await _operationStorageProvider().GetOperations();
        return Convert(operations);
    }

    /// <summary>
    /// Get and observe all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="OperationInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<OperationInformation>>> AllOperations(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<OperationInformation>>();
        var observable = _operationStorageProvider().ObserveOperations();
        var subscription = observable.Subscribe(operations => clientObservable.OnNext(Convert(operations)));
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

    IEnumerable<OperationInformation> Convert(IEnumerable<OperationState> operations) =>
         operations.Select(_ => new OperationInformation(_.Id, _.Name, _.Details, _.Type)).ToArray();
}
