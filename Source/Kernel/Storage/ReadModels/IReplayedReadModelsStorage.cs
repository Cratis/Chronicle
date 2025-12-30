// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.ReadModels;

/// <summary>
/// Defines the storage for replayed read models.
/// </summary>
public interface IReplayedReadModelsStorage
{
    /// <summary>
    /// Store the fact that a read model has been replayed.
    /// </summary>
    /// <param name="occurrence">The <see cref="ReadModelOccurrence"/> to store.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(ReadModelOccurrence occurrence);

    /// <summary>
    /// Get all replayed read model occurrences for a specific read model.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> to get occurrences for.</param>
    /// <returns>Collection of replayed read model occurrences.</returns>
    Task<IEnumerable<ReadModelOccurrence>> GetOccurrences(ReadModelIdentifier readModel);
}
