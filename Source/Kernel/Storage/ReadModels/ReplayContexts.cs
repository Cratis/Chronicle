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
    public async Task<ReplayContext> Establish(ReadModelType type, ReadModelName readModelName)
    {
        var replayStarted = DateTimeOffset.UtcNow;
        var rewoundCollectionsPrefix = $"{readModelName}-";
        var revertModelName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}";
        var context = new ReplayContext(type, readModelName, revertModelName, replayStarted);
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
    public Task Evict(ReadModelIdentifier readModel) =>
        storage.Remove(readModel);
}
