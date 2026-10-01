// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayContexts"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
public class ReplayContexts(IReplayContextsStorage storage) : IReplayContexts
{
    readonly ConcurrentDictionary<ReadModelIdentifier, ReplayContext> _contexts = new();

    /// <inheritdoc/>
    public async Task<ReplayContext> Establish(ReadModelType type, ReadModelContainerName containerName)
    {
        var replayStarted = DateTimeOffset.UtcNow;
        var rewoundCollectionsPrefix = $"{containerName}-";

        // The time alone names two replays of one read model started in the same second alike, and the second one's
        // promotion then fails renaming the read model to a revert container that already exists.
        var revertContainerName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        var context = new ReplayContext(type, containerName, revertContainerName, replayStarted);
        _contexts[type.Identifier] = context;
        await storage.Save(context);
        return context;
    }

    /// <inheritdoc/>
    public async Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel)
    {
        if (_contexts.TryGetValue(readModel, out var context))
        {
            return Result.Success<ReplayContext, GetContextError>(context);
        }

        var result = await storage.TryGet(readModel);
        return result.Match(
            Result.Success<ReplayContext, GetContextError>,
            Result.Failed<ReplayContext, GetContextError>);
    }

    /// <inheritdoc/>
    public Task Evict(ReadModelIdentifier readModel)
    {
        // The cache is consulted before storage, so a context left in it outlives the replay it belongs to.
        // The next replay's end would then find that stale context - with the previous replay's revert
        // container name, which already exists - and fail the swap (#4296).
        _contexts.TryRemove(readModel, out _);
        return storage.Remove(readModel);
    }
}
