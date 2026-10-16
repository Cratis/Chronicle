// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Defines cluster-serialized initialization for one event store.
/// </summary>
public interface IEventStoreInitializer : IGrainWithStringKey
{
    /// <summary>
    /// Ensures first-time setup has completed, retrying stores without a durable completion marker.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();
}
