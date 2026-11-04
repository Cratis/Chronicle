// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Limits concurrent gRPC reactor and reducer registrations on this silo.
/// </summary>
public interface IObserverRegistrationGate
{
    /// <summary>
    /// Wait for permission to register an observer.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the registering client disconnects.</param>
    /// <returns>A lease that releases the permit when disposed.</returns>
    ValueTask<IDisposable> Enter(CancellationToken cancellationToken);
}
