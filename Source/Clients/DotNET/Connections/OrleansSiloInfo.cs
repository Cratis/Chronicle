// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Data.Tables;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents the silo info found in Azure Storage.
/// </summary>
public class OrleansSiloInfo : ITableEntity
{
    /// <inheritdoc/>
    public string PartitionKey { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string RowKey { get; set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset? Timestamp { get; set; }

    /// <inheritdoc/>
    public ETag ETag { get; set; }

    /// <summary>
    /// Gets the host name of the silo.
    /// </summary>
    public string Address { get; set; } = string.Empty;
}
