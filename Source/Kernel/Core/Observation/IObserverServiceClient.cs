// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Services;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a client for <see cref="IObserverService"/>.
/// </summary>
public interface IObserverServiceClient : IGrainServiceClient<IObserverService>, IObserverService
{
    /// <summary>
    /// Flush every active silo, retaining each silo's identity to detect a lost bulk window.
    /// </summary>
    /// <param name="observerDetails">The observer to flush.</param>
    /// <param name="replay">Whether the observer is replaying.</param>
    /// <returns>The window identities reported by each silo.</returns>
    Task<IReadOnlyDictionary<SiloAddress, Guid[]>> FlushAllFor(ObserverDetails observerDetails, bool replay);
}
