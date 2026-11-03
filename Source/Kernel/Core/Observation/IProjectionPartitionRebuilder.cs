// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a subscriber that rebuilds one projection partition out of sight of the live read model and publishes it atomically.
/// </summary>
/// <remarks>
/// One activation serves one partition replay job step. Events delivered through <see cref="IObserverSubscriber.OnNext"/>
/// build the partition's read model in memory only; nothing reaches the live sink until <see cref="Publish"/>.
/// </remarks>
public interface IProjectionPartitionRebuilder : IObserverSubscriber, IAmOwnedByKernel
{
    /// <summary>
    /// Begin a rebuild from empty, discarding anything a previous attempt built.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the projection being rebuilt.</param>
    /// <param name="partition">The <see cref="Key"/> of the partition being rebuilt.</param>
    /// <returns>Awaitable task.</returns>
    Task Begin(ObserverKey observerKey, Key partition);

    /// <summary>
    /// Publish the rebuilt partition to the live read model, replacing what it holds for the partition.
    /// </summary>
    /// <param name="watermark">The last event sequence number the rebuild covers.</param>
    /// <returns>True if the live read model now holds the rebuild; false if a newer write superseded it and nothing was published.</returns>
    Task<bool> Publish(EventSequenceNumber watermark);
}
