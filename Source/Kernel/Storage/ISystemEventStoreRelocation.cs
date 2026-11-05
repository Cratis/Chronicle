// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines a system that moves the System event store's databases between the names they can have.
/// </summary>
public interface ISystemEventStoreRelocation
{
    /// <summary>
    /// Move the System event store's databases to a <see cref="SystemEventStoreLocation"/>.
    /// </summary>
    /// <param name="location">The <see cref="SystemEventStoreLocation"/> to move to.</param>
    /// <returns>Awaitable task.</returns>
    Task MoveTo(SystemEventStoreLocation location);

    /// <summary>
    /// Finds System event store data that sits under the name the kernel is not configured to use.
    /// </summary>
    /// <returns>A description of where the data is and where the kernel looks, or null when nothing is misplaced.</returns>
    Task<string?> FindMisplacedData();
}
