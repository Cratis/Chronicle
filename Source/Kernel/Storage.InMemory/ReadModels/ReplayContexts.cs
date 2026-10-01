// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.ReadModels;

/// <summary>
/// Represents an in-memory implementation of <see cref="IReplayContexts"/>.
/// </summary>
public sealed class ReplayContexts : IReplayContexts
{
    readonly ConcurrentDictionary<ReadModelIdentifier, ReplayContext> _contexts = new();

    /// <inheritdoc/>
    public Task<ReplayContext> Establish(ReadModelType type, ReadModelContainerName containerName)
    {
        var replayStarted = DateTimeOffset.UtcNow;
        var rewoundCollectionsPrefix = $"{containerName}-";

        // The time alone names two replays of one read model started in the same second alike, and the second one's
        // promotion then fails renaming the read model to a revert container that already exists.
        var revertContainerName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        var context = new ReplayContext(type, containerName, revertContainerName, replayStarted);
        _contexts[type.Identifier] = context;
        return Task.FromResult(context);
    }

    /// <inheritdoc/>
    public Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel) =>
        Task.FromResult(
            _contexts.TryGetValue(readModel, out var context)
                ? Result.Success<ReplayContext, GetContextError>(context)
                : Result.Failed<ReplayContext, GetContextError>(GetContextError.NotFound));

    /// <inheritdoc/>
    public Task Save(ReplayContext context)
    {
        _contexts[context.Type.Identifier] = context;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Evict(ReadModelIdentifier readModel)
    {
        _contexts.TryRemove(readModel, out _);
        return Task.CompletedTask;
    }
}
