// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Jobs;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cratis.Chronicle.Grains.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling job state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStepGrainStorageProvider"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
public class JobStepGrainStorageProvider(IStorage storage) : IGrainStorage
{
    /// <inheritdoc/>
    public Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));

        if (grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            var key = (JobStepKey)keyExtension!;
            var @namespace = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;
            var state = await @namespace.Read<T>(key.JobId, jobStepId);
            if (state is not null)
            {
                grainState.State = state;
            }
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(T));

        if (grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            var key = (JobStepKey)keyExtension!;
            var @namespace = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;

            var actualState = (grainState.State as JobStepState)!;

            switch (GetStatus(actualState))
            {
                case JobStepStatus.Failed:
                    await @namespace.MoveToFailed(key.JobId, jobStepId, grainState.State);
                    break;

                case JobStepStatus.Stopped:
                case JobStepStatus.Succeeded:
                    await @namespace.Remove(key.JobId, jobStepId);
                    break;

                case JobStepStatus.Paused:
                case JobStepStatus.Running:
                case JobStepStatus.Scheduled:
                    await @namespace.Save(key.JobId, jobStepId, grainState.State);
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
