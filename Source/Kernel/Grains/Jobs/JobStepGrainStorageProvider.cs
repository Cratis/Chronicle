// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using OneOf.Types;
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
        ThrowIfInvalidJobStateType(typeof(T), nameof(ClearStateAsync));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        const string methodName = nameof(WriteStateAsync);
        ThrowIfInvalidJobStateType(type, methodName);

        if (grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            var key = (JobStepKey)keyExtension!;
            var jobStepStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;

            var readStateResult = await jobStepStorage.Read<T>(key.JobId, jobStepId);

            await readStateResult.Match(
                result => HandleSuccessfulRead(result, grainState),
                _ => Task.CompletedTask,
                error => Task.FromException(new JobStepGrainStorageProviderError(type, error, nameof(ReadStateAsync))));
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        const string methodName = nameof(WriteStateAsync);

        ThrowIfInvalidJobStateType(type, methodName);

        if (!grainId.TryGetGuidKey(out var jobStepId, out var keyExtension))
        {
            return;
        }

        var key = (JobStepKey)keyExtension!;
        var jobStepStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;

        var actualState = (grainState.State as JobStepState)!;

        switch (GetStatus(actualState))
        {
            case JobStepStatus.Failed:
                await HandleCatchNone(jobStepStorage.MoveToFailed(key.JobId, jobStepId, grainState.State), type, methodName);
                break;

            case JobStepStatus.Stopped:
            case JobStepStatus.Succeeded:
                await HandleCatch(jobStepStorage.Remove(key.JobId, jobStepId), type, methodName);
                break;

            case JobStepStatus.Paused:
            case JobStepStatus.Running:
            case JobStepStatus.Scheduled:
                await HandleCatchNone(jobStepStorage.Save(key.JobId, jobStepId, grainState.State), type, methodName);
                break;
        }
        return;

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

    static void ThrowIfInvalidJobStateType(Type type, string operationName) => JobStepStateType
        .Verify(type)
        .Switch(_ => { }, error => throw new JobStepGrainStorageProviderError(type, error, operationName));

    static async Task HandleCatchNone(Task<Catch<None, Storage.Jobs.JobStepError>> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(
            _ => Task.CompletedTask,
            error => Task.FromException(new JobStepGrainStorageProviderError(type, error, methodName)),
            error => Task.FromException(new JobStepGrainStorageProviderError(type, error, methodName)));
    }

    static async Task HandleCatch(Task<Catch> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(_ => Task.CompletedTask, error => Task.FromException(new JobStepGrainStorageProviderError(type, error, methodName)));
    }

    Task HandleSuccessfulRead<T>(T state, IGrainState<T> grainState)
    {
        grainState.State = state;
        return Task.CompletedTask;
    }
}
