// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Generations.Enablement;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Refuses legacy full replay of a projection once generation initialization has left durable receipts.
/// </summary>
internal static class GenerationInitializationReplay
{
    /// <summary>
    /// Reads the progress that applies to a projection observer; other observer types are never affected.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="key">The observer.</param>
    /// <param name="type">The kind of observer.</param>
    /// <returns>The progress, or <see cref="GenerationInitializationProgress.NotStarted"/> for non-projections without reading.</returns>
    internal static Task<GenerationInitializationProgress> GetProgress(IStorage storage, ObserverKey key, ObserverType type) =>
        type == ObserverType.Projection
            ? storage.GetEventStore(key.EventStore).GenerationInitialization.GetProgress()
            : Task.FromResult(GenerationInitializationProgress.NotStarted);

    /// <summary>
    /// Refuses a new full replay once any initialization receipt exists.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="key">The observer being replayed.</param>
    /// <param name="type">The kind of observer.</param>
    /// <returns>A task representing the check.</returns>
    /// <exception cref="GenerationInitializationReplayNotSupported">Initialization has begun.</exception>
    internal static async Task EnsureStartAllowed(IStorage storage, ObserverKey key, ObserverType type)
    {
        if (await GetProgress(storage, key, type) != GenerationInitializationProgress.NotStarted)
        {
            throw new GenerationInitializationReplayNotSupported(GenerationInitializationReplayNotSupported.StartRefusedReason);
        }
    }

    /// <summary>
    /// Refuses a new full replay once any initialization receipt exists, but lets an existing replay job be resumed
    /// until a materialization is adopted, so replay work started before initialization can drain.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="key">The observer being replayed.</param>
    /// <param name="type">The kind of observer.</param>
    /// <param name="hasExistingReplayJob">Looks up whether the observer already owns an unfinished replay job; only called while quiescing.</param>
    /// <returns>A task representing the check.</returns>
    /// <exception cref="GenerationInitializationReplayNotSupported">A new replay would start, or a materialization was adopted.</exception>
    internal static async Task EnsureStartOrResumeAllowed(IStorage storage, ObserverKey key, ObserverType type, Func<Task<bool>> hasExistingReplayJob)
    {
        var progress = await GetProgress(storage, key, type);
        if (progress == GenerationInitializationProgress.NotStarted)
        {
            return;
        }

        if (progress == GenerationInitializationProgress.Quiescing && await hasExistingReplayJob())
        {
            return;
        }

        throw new GenerationInitializationReplayNotSupported(GenerationInitializationReplayNotSupported.StartRefusedReason);
    }

    /// <summary>
    /// Refuses to continue or complete a persisted replay once a materialization has been adopted.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="key">The observer being replayed.</param>
    /// <param name="type">The kind of observer.</param>
    /// <returns>A task representing the check.</returns>
    /// <exception cref="GenerationInitializationReplayNotSupported">A materialization was adopted.</exception>
    internal static async Task EnsureContinueAllowed(IStorage storage, ObserverKey key, ObserverType type)
    {
        if (await GetProgress(storage, key, type) == GenerationInitializationProgress.Adopted)
        {
            throw new GenerationInitializationReplayNotSupported(GenerationInitializationReplayNotSupported.ContinueRefusedReason);
        }
    }

    /// <summary>
    /// Refuses a persisted job permanently, retaining the same failure history as preparation errors.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="request">The replay request of the job.</param>
    /// <param name="state">The job state to fail on refusal.</param>
    /// <param name="persist">Persists the terminal failure before returning to the job framework.</param>
    /// <returns>A task representing the check.</returns>
    internal static async Task EnsureContinueAllowed(IStorage storage, ReplayObserverRequest request, JobState state, Func<Task> persist)
    {
        try
        {
            await EnsureContinueAllowed(storage, request.ObserverKey, request.ObserverType);
        }
        catch (GenerationInitializationReplayNotSupported exception)
        {
            if (state.Status != JobStatus.Failed)
            {
                state.Status = JobStatus.Failed;
                state.StatusChanges.Add(new()
                {
                    Status = JobStatus.Failed,
                    Occurred = DateTimeOffset.UtcNow,
                    ExceptionMessages = [exception.Message],
                    ExceptionStackTrace = exception.StackTrace ?? string.Empty
                });
                await persist();
            }

            throw;
        }
    }
}
