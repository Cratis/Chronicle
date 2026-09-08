// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Servers;

/// <summary>
/// Represents a live snapshot of a server process' CPU and memory usage.
/// </summary>
public record ServerInstanceMetrics
{
    /// <summary>
    /// Gets the percentage of available CPU capacity the process used over the sampling window.
    /// </summary>
    public double CpuUsagePercentage { get; init; }

    /// <summary>
    /// Gets the process' working set, in bytes.
    /// </summary>
    public long WorkingSetBytes { get; init; }
}
