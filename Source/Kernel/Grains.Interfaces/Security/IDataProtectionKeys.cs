// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Defines a grain that manages Data Protection keys for multi-instance deployments.
/// This grain ensures consistent key management across cluster nodes.
/// </summary>
public interface IDataProtectionKeys : IGrainWithStringKey
{
    /// <summary>
    /// Gets all stored Data Protection key XML elements.
    /// </summary>
    /// <returns>Collection of XML strings representing the keys.</returns>
    Task<IEnumerable<string>> GetAllKeys();

    /// <summary>
    /// Stores a new Data Protection key.
    /// </summary>
    /// <param name="friendlyName">The friendly name of the key.</param>
    /// <param name="xml">The XML representation of the key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreKey(string friendlyName, string xml);
}
