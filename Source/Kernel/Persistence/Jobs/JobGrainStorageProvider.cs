// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Persistence.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling job state storage.
/// </summary>
public class JobGrainStorageProvider : IGrainStorage
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public JobGrainStorageProvider(
        IServiceProvider serviceProvider,
        IExecutionContextManager executionContextManager)
    {
        _serviceProvider = serviceProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var storage = _serviceProvider.GetRequiredService<IJobStorage<T>>();
            var state = await storage.Read(jobId);
            if (state is not null)
            {
                grainState.State = state;
            }
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var storage = _serviceProvider.GetRequiredService<IJobStorage<T>>();
            var stepStorage = _serviceProvider.GetRequiredService<IJobStepStorage>();
            var state = (grainState.State as IJobState)!;

            if (state.Remove)
            {
                await stepStorage.RemoveAllForJob(jobId);
                await storage.Remove(jobId);
            }
            else
            {
                await storage.Save(jobId, grainState.State);
            }

            if (state.Status == JobStatus.CompletedSuccessfully)
            {
                await stepStorage.RemoveAllForJob(jobId);
            }
        }
    }
}
