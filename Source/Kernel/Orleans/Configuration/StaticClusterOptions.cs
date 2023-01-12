// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Extensions.Orleans.Configuration;

#pragma warning disable CA1819

/// <summary>
/// Represents the options for the static cluster configuration.
/// </summary>
public class StaticClusterOptions
{
    /// <summary>
    /// Gets the IP address of the primary silo.
    /// </summary>
    public string PrimarySiloIP { get; init; } = string.Empty;

    /// <summary>
    /// Gets the port of the primary silo.
    /// </summary>
    public int PrimarySiloPort { get; init; } = 11111;

    /// <summary>
    /// Gets the <see cref="EndPoint"/> configurations for all Orleans silos running.
    /// </summary>
    public EndPoint[] Gateways { get; init; } = Array.Empty<EndPoint>();
}
