// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Concurrency;

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Defines the single grain that publishes the open incident gauges.
/// </summary>
public interface IAlertIncidentsMetricsOwner : IGrainWithIntegerKey
{
    /// <summary>
    /// Ensures the grain is active. Interleaved so a slow refresh cannot time out the pings.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    [AlwaysInterleave]
    Task Ensure();
}
