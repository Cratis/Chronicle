// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Defines a catch up process for an observer.
/// </summary>
public interface IReplay : IGrainWithGuidCompoundKey
{
    /// <summary>
    /// Starts a replay process for a given observer.
    /// </summary>
    /// <param name="subscription">The <see cref="ObserverSubscription"/> to use to catch up.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(ObserverSubscription subscription);

    /// <summary>
    /// Stop replaying.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Stop();
}
