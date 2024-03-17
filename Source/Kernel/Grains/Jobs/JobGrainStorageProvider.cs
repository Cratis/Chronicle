// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Jobs;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Jobs;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Kernel.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling job state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class JobGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var jobStepStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;

            await jobStepStorage.RemoveAllForJob(jobId);
            await jobStorage.Remove(jobId);
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;

            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var state = await jobStorage.Read<T>(jobId);
            if (state is not null)
            {
                var jobState = (state as JobState)!;
                var jobStepStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;
                var successfulCount = await jobStepStorage.CountForJob(jobState.Id, JobStepStatus.Succeeded);
                var failedCount = await jobStepStorage.CountForJob(jobState.Id, JobStepStatus.Failed);

                var hasChanges = jobState.Progress.SuccessfulSteps != successfulCount ||
                                 jobState.Progress.FailedSteps != failedCount;

                jobState.Progress.SuccessfulSteps = successfulCount;
                jobState.Progress.FailedSteps = failedCount;
                grainState.State = state;

                if (hasChanges)
                {
                    await jobStorage.Save(jobId, grainState.State);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var state = (grainState.State as JobState)!;
            state.Id = jobId;

            await jobStorage.Save(jobId, grainState.State);
        }
    }
}
