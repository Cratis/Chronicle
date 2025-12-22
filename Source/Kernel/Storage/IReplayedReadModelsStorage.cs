// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the storage for replayed read models.
/// </summary>
public interface IReplayedReadModelsStorage
{
    /// <summary>
    /// Store the fact that a read model has been replayed.
    /// </summary>
    /// <param name="observer">The <see cref="ObserverId"/> for the observer.</param>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(ObserverId observer, ReplayContext context);

    /// <summary>
    /// Get all replayed read model occurrences for a specific read model.
    /// </summary>
    /// <param name="readModel">The <see cref="ReadModelName"/> to get occurrences for.</param>
    /// <returns>Collection of replayed read model occurrences.</returns>
    Task<IEnumerable<ReplayedReadModelOccurrence>> GetOccurrences(ReadModelName readModel);
}
