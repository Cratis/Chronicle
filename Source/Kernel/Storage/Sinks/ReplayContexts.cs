// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Models;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IReplayContexts"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
public class ReplayContexts(IReplayContextsStorage storage) : IReplayContexts
{
    readonly ConcurrentDictionary<ModelName, ReplayContext> _contexts = new();

    /// <inheritdoc/>
    public async Task<ReplayContext> Establish(ModelName model)
    {
        var replayStarted = DateTimeOffset.UtcNow;
        var rewoundCollectionsPrefix = $"{model}-";
        var revertModelName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}";
        var context = new ReplayContext(model, revertModelName, replayStarted);
        _contexts[model] = context;
        await storage.Save(context);
        return context;
    }

    /// <inheritdoc/>
    public async Task<Result<ReplayContext, GetContextError>> TryGet(ModelName model)
    {
        if (_contexts.TryGetValue(model, out var context))
        {
            return Result.Success<ReplayContext, GetContextError>(context);
        }

        var result = await storage.TryGet(model);
        return result.Match(
            Result.Success<ReplayContext, GetContextError>,
            Result.Failed<ReplayContext, GetContextError>);
    }

    /// <inheritdoc/>
    public Task Evict(ModelName model) =>
        storage.Remove(model);
}
