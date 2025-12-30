// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Defines the manager for managing read model replays.
/// </summary>
public interface IReadModelReplayManager : IGrainWithStringKey
{
     /// <summary>
    /// Record a replayed read model occurrence.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> for the observer.</param>
    /// <param name="context">The <see cref="ReplayContext"/> for the replay.</param>
    /// <returns>Awaitable task.</returns>
    Task Replayed(ObserverId observerId, ReplayContext context);

    /// <summary>
    /// Get all replayed read model occurrences.
    /// </summary>
    /// <returns>Collection of <see cref="ReadModelOccurrence"/>.</returns>
    Task<IEnumerable<ReadModelOccurrence>> GetOccurrences();
}
