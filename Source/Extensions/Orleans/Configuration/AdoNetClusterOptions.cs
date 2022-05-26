// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

/// <summary>
/// Represents the options for the Azure Storage clustering.
/// </summary>
public class AdoNetClusterOptions
{
    /// <summary>
    /// Gets the connection string to use.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>
    /// Gets the invariant name of the connector to use.
    /// </summary>
    public string Invariant { get; init; } = string.Empty;
}
