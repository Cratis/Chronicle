// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;

namespace Aksio.Cratis.Metrics;

/// <summary>
/// Holds the global metrics for the system.
/// </summary>
public static class GlobalMetrics
{
    /// <summary>
    /// Gets the global <see cref="Meter"/>.
    /// </summary>
    public static readonly Meter Meter = new("Cratis");
}
