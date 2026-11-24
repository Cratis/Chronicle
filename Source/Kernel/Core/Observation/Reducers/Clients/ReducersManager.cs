// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducersManager"/>.
/// </summary>
/// <remarks>
/// Reducer definitions are stored per event store when a client registers them, so the manager holds no state of its own
/// and answers from that storage.
/// </remarks>
/// <param name="storage">The storage for persisted reducer definitions.</param>
public class ReducersManager(IStorage storage) : Grain, IReducersManager
{
    /// <inheritdoc/>
    public Task<bool> HasReducerForReadModel(ReadModelIdentifier readModel) =>
        storage.GetEventStore((EventStoreName)this.GetPrimaryKeyString()).Reducers.HasForReadModel(readModel);
}
