// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;
using Orleans.Storage;

namespace Aksio.Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling job state storage.
/// </summary>
public class JobStepGrainStorageProvider : IGrainStorage
{
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobGrainStorageProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public JobStepGrainStorageProvider(
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
        if (grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            var key = (JobStepKey)keyExtension!;
            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var storage = _serviceProvider.GetRequiredService<IJobStepStorage<T>>();
            var state = await storage.Read(key.JobId, jobStepId);
            if (state is not null)
            {
                grainState.State = state;
            }
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        if (grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            var key = (JobStepKey)keyExtension!;
            _executionContextManager.Establish(key.TenantId, CorrelationId.New(), key.MicroserviceId);
            var storage = _serviceProvider.GetRequiredService<IJobStepStorage<T>>();

            var actualState = (grainState.State as JobStepState)!;

            switch (GetStatus(actualState))
            {
                case JobStepStatus.Failed:
                    await storage.MoveToFailed(key.JobId, jobStepId, grainState.State);
                    break;

                case JobStepStatus.Stopped:
                case JobStepStatus.Succeeded:
                    await storage.Remove(key.JobId, jobStepId);
                    break;

                case JobStepStatus.Paused:
                case JobStepStatus.Running:
                case JobStepStatus.Scheduled:
                    await storage.Save(key.JobId, jobStepId, grainState.State);
                    break;
            }
        }

        static JobStepStatus GetStatus(JobStepState state)
        {
            if (IsStopped(state)) return JobStepStatus.Stopped;
            if (IsSuccessful(state)) return JobStepStatus.Succeeded;
            if (IsFailed(state)) return JobStepStatus.Failed;
            if (IsPaused(state)) return JobStepStatus.Paused;
            return JobStepStatus.Running;
        }

        static bool IsPaused(JobStepState state) =>
            state.StatusChanges.Count > 0 &&
            state.StatusChanges[^1].Status == JobStepStatus.Paused;

        static bool IsStopped(JobStepState state) =>
            state.StatusChanges.Count > 0 &&
            state.StatusChanges[^1].Status == JobStepStatus.Stopped;

        static bool IsSuccessful(JobStepState state) =>
                state.StatusChanges.Count > 1 &&
                state.StatusChanges[^1].Status == JobStepStatus.Succeeded &&
                state.StatusChanges[^2].Status == JobStepStatus.Running;

        static bool IsFailed(JobStepState state) =>
                state.StatusChanges.Count > 1 &&
                state.StatusChanges[^1].Status == JobStepStatus.Failed &&
                state.StatusChanges[^2].Status == JobStepStatus.Running;
    }
}
