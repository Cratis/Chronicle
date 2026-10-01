// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayContexts"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
public class ReplayContexts(IReplayContextsStorage storage) : IReplayContexts
{
    /// <inheritdoc/>
    public async Task<ReplayContext> Establish(ReadModelType type, ReadModelContainerName containerName)
    {
        var replayStarted = DateTimeOffset.UtcNow;
        var rewoundCollectionsPrefix = $"{containerName}-";

        // The time alone names two replays of one read model started in the same second alike, and the second one's
        // promotion then fails renaming the read model to a revert container that already exists.
        var revertContainerName = $"{rewoundCollectionsPrefix}{replayStarted:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
        var context = new ReplayContext(type, containerName, revertContainerName, replayStarted);
        await Save(context);
        return context;
    }

    /// <inheritdoc/>
    public Task<Result<ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel) => storage.TryGet(readModel);

    /// <inheritdoc/>
    public Task Save(ReplayContext context) => storage.Save(context);

    /// <inheritdoc/>
    public Task Evict(ReadModelIdentifier readModel) => storage.Remove(readModel);
}
