// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Defines storage for Data Protection keys.
/// </summary>
public interface IDataProtectionKeyStorage
{
    /// <summary>
    /// Gets all stored Data Protection keys.
    /// </summary>
    /// <returns>Collection of <see cref="DataProtectionKey"/>.</returns>
    Task<IEnumerable<DataProtectionKey>> GetAll();

    /// <summary>
    /// Stores a new Data Protection key.
    /// </summary>
    /// <param name="key">The key to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Store(DataProtectionKey key);
}
