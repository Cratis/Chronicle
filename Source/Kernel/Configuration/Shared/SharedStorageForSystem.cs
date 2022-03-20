// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the shared storage configuration for all <see cref="StorageType">storage types</see> within the system.
/// </summary>
public class SharedStorageForSystem : Dictionary<string, SharedStorageType>
{
    /// <summary>
    /// Get a specific <see cref="StorageType"/>.
    /// </summary>
    /// <param name="storageType">Type of storage to get.</param>
    /// <returns><see cref="StorageType"/> instance.</returns>
    public SharedStorageType Get(string storageType) => this[storageType];
}
