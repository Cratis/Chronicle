// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Configuration;

/// <summary>
/// Represents the configuration options for Azure Storage clustering.
/// </summary>
/// <remarks>
/// Azure Storage cluster options is based on the Orleans Silo instance tables.
/// </remarks>
public class AzureStorageClusterOptions
{
    /// <summary>
    /// The default table name for the Orleans Silo instances.
    /// </summary>
    public const string DEFAULT_TABLE_NAME = "OrleansSiloInstances";

    /// <summary>
    /// Gets the connection string to use.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets the table name to use.
    /// </summary>
    public string TableName { get; set; } = DEFAULT_TABLE_NAME;

    /// <summary>
    /// Gets the port to connect to, defaults to 80.
    /// </summary>
    public int Port { get; set; } = 80;

    /// <summary>
    /// Gets whether or not to use TLS for connecting (https).
    /// </summary>
    public bool Secure { get; set; }
}
