// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Refuses namespace-local full replay of a projection whose sink is shared across namespaces.
/// </summary>
internal static class GlobalProjectionReplay
{
    /// <summary>
    /// The explanation returned for an unsafe namespace-local rebuild.
    /// </summary>
    internal const string RefusalReason = "Namespace-local replay of a global projection is not supported. Its shared sink requires a coordinated rebuild across all namespaces. No shared-sink replay operation was performed.";

    /// <summary>
    /// Refuse full replay before the observer changes its progress or running state.
    /// </summary>
    /// <param name="storage">Storage containing the current projection definitions.</param>
    /// <param name="key">The observer being replayed.</param>
    /// <param name="type">The kind of observer.</param>
    /// <returns>A task representing the check.</returns>
    /// <exception cref="GlobalProjectionReplayNotSupported">The projection has a shared global sink.</exception>
    internal static async Task EnsureSupported(IStorage storage, ObserverKey key, ObserverType type)
    {
        if (type != ObserverType.Projection)
        {
            return;
        }

        var definitions = storage.GetEventStore(key.EventStore).Projections;
        if (await definitions.Has(key.ObserverId) && (await definitions.Get(key.ObserverId)).Scope is ProjectionScope.Global or ProjectionScope.Both)
        {
            throw new GlobalProjectionReplayNotSupported();
        }
    }

    /// <summary>
    /// Refuses an unsupported persisted job permanently, retaining the same failure history as preparation errors.
    /// </summary>
    /// <param name="storage">Storage containing the projection definitions.</param>
    /// <param name="key">The observer being replayed.</param>
    /// <param name="type">The kind of observer.</param>
    /// <param name="state">The job state to fail on refusal.</param>
    /// <param name="persist">Persists the terminal failure before returning to the job framework.</param>
    /// <returns>A task representing the check.</returns>
    internal static async Task EnsureSupported(IStorage storage, ObserverKey key, ObserverType type, JobState state, Func<Task> persist)
    {
        try
        {
            await EnsureSupported(storage, key, type);
        }
        catch (GlobalProjectionReplayNotSupported exception)
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
            throw;
        }
    }
}
