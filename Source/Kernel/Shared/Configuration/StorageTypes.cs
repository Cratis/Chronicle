// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration;

/// <summary>
/// Represents the shared storage configuration for all <see cref="StorageType">storage types</see> within the system.
/// </summary>
public class StorageTypes : Dictionary<string, StorageType>
{
    /// <summary>
    /// Get a specific <see cref="StorageType"/>.
    /// </summary>
    /// <param name="storageType">Type of storage to get.</param>
    /// <returns><see cref="StorageType"/> instance.</returns>
    public StorageType Get(string storageType) => this[storageType];
}
