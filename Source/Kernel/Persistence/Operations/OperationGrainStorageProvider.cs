// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Operations;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Persistence.Operations;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for <see cref="OperationState"/>.
/// </summary>
public class OperationGrainStorageProvider : IGrainStorage
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public OperationGrainStorageProvider(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var operationId, out var keyExtension))
        {
            var key = (OperationKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var operationStorage = _serviceProvider.GetRequiredService<IOperationStorage>();
            await operationStorage.Remove(operationId);
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var operationId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<OperationState>)!;
            var key = (OperationKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var operationStorage = _serviceProvider.GetRequiredService<IOperationStorage>();
            actualGrainState.State = await operationStorage.Get(operationId) ?? new OperationState();
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var operationId, out var keyExtension))
        {
            var actualGrainState = (grainState as IGrainState<OperationState>)!;
            var key = (OperationKey)keyExtension!;

            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var operationStorage = _serviceProvider.GetRequiredService<IOperationStorage>();
            await operationStorage.Save(operationId, actualGrainState.State);
        }
    }
}
