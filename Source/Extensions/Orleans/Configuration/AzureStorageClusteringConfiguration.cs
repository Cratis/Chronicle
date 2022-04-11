// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents cluster configuration for azure storage.
/// </summary>
public class AzureStorageClusteringConfiguration
{
    /// <summary>
    /// Gets the connection string.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public string TableName { get; init; } = string.Empty;
}
