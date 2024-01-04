// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Storage;

/// <summary>
/// Defines the storage for the cluster level.
/// </summary>
public interface IClusterStorage
{
    /// <summary>
    /// Get the <see cref="IEventStoreStorage"/> for a specific <see cref="EventStore"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStore"/> to get.</param>
    /// <returns>The <see cref="IEventStoreStorage"/> instance.</returns>
    IEventStoreStorage GetEventStore(EventStore eventStore);
}
