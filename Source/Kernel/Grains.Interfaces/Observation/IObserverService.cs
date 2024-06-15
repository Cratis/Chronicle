// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Services;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Defines a service that lives in each silo and can be called to notify about replay state changes for observers.
/// </summary>
public interface IObserverService : IGrainService
{
    /// <summary>
    /// Begin replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task BeginReplayFor(ObserverDetails observerDetails);

    /// <summary>
    /// End replay for a specific observer.
    /// </summary>
    /// <param name="observerDetails">The <see cref="ObserverDetails"/> for the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task EndReplayFor(ObserverDetails observerDetails);
}
