// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Defines the contract for working with event stores.
/// </summary>
[Service]
public interface IEventStores
{
    /// <summary>
    /// Get all available event stores.
    /// </summary>
    /// <returns>Collection of strings representing the names of the event stores.</returns>
    [Operation]
    Task<IEnumerable<string>> GetEventStores();

    /// <summary>
    /// Observe all available event stores.
    /// </summary>
    /// <returns>Observable of all event stores.</returns>
    [Operation]
    IObservable<IEnumerable<string>> ObserveEventStores();
}
