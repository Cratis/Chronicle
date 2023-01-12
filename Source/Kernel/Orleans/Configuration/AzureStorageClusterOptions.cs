// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Clustering.AzureStorage;

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the options for the Azure Storage clustering.
/// </summary>
public class AzureStorageClusterOptions
{
    /// <summary>
    /// Gets the connection string to use.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets the table name to use.
    /// </summary>
    public string TableName { get; init; } = AzureStorageClusteringOptions.DEFAULT_TABLE_NAME;
}
