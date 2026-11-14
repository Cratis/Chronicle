// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that clears the applied event markers a partition left, so that a partition replay rewrites it.
/// </summary>
public interface IPartitionReplayMarkers
{
    /// <summary>
    /// Clears the markers of the partition for an observer, when the observer is a projection that keeps markers.
    /// </summary>
    /// <remarks>Idempotent. Does nothing for observers that are not projections and for projections without markers.</remarks>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> of the replayed observer.</param>
    /// <param name="partition">The replayed partition.</param>
    /// <returns>Awaitable task.</returns>
    Task ClearFor(ObserverDetails observerDetails, Key partition);
}
