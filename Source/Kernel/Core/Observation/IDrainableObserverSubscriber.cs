// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a subscriber whose non-interleavable barrier waits for earlier deliveries to finish.
/// </summary>
public interface IDrainableObserverSubscriber : IObserverSubscriber
{
    /// <summary>
    /// Waits until earlier deliveries on this activation have finished.
    /// </summary>
    /// <returns>A task representing the barrier.</returns>
    Task Drain();
}
