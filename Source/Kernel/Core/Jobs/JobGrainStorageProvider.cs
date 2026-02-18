// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using OneOf.Types;
using Orleans.Storage;

namespace Cratis.Chronicle.Jobs;

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
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(ClearStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var jobStepStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).JobSteps;

            await HandleCatch(jobStepStorage.RemoveAllForJob(jobId), type, nameof(ClearStateAsync));
            await HandleCatch(jobStorage.Remove(jobId), type, nameof(ClearStateAsync));
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(ReadStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;

            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var readStateResult = await jobStorage.Read<T>(jobId);
            await readStateResult.Match(
                state =>
                {
                    grainState.State = state;
                    return Task.CompletedTask;
                },
                _ => Task.CompletedTask,
                error => Task.FromException(new JobGrainStorageProviderError(type, error, nameof(ReadStateAsync))));
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(WriteStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var jobStorage = storage.GetEventStore(key.EventStore).GetNamespace(key.Namespace).Jobs;
            var state = (grainState.State as JobState)!;
            state.Id = jobId;
            await HandleCatchNone(jobStorage.Save(jobId, grainState.State), type, nameof(WriteStateAsync));
        }
    }

    static void ThrowIfInvalidJobStateType(Type type, string operationName) => JobStateType
        .Verify(type)
        .Switch(_ => { }, error => throw new JobGrainStorageProviderError(type, error, operationName));

    static async Task HandleCatchNone(Task<Catch<None, Storage.Jobs.JobError>> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(
            _ => Task.CompletedTask,
            error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)),
            error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)));
    }

    static async Task HandleCatch(Task<Catch> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(_ => Task.CompletedTask, error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)));
    }
}
