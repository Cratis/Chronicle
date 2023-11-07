// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Observation.Replaying;

namespace Aksio.Cratis.Kernel.Persistence.Observation.Replaying;

/// <summary>
/// Defines a storage system for working with <see cref="ReplayCandidate"/>.
/// </summary>
public interface IReplayCandidatesStorage
{
    /// <summary>
    /// Add a candidate for replaying.
    /// </summary>
    /// <param name="replayCandidate"><see cref="ReplayCandidate"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(ReplayCandidate replayCandidate);
}
