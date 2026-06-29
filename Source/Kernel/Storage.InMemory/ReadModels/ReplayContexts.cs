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
        var revertContainerName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}";
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
    public Task Evict(ReadModelIdentifier readModel)
    {
        _contexts.TryRemove(readModel, out _);
        return Task.CompletedTask;
    }
}
