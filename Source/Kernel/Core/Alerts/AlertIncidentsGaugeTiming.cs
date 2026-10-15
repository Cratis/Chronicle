// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts;

/// <summary>
/// Holds the timings of the open incident gauge.
/// </summary>
internal static class AlertIncidentsGaugeTiming
{
    /// <summary>How often the owner grain refreshes the snapshot from storage.</summary>
    internal static readonly TimeSpan RefreshPeriod = TimeSpan.FromSeconds(30);

    /// <summary>How often every silo pings the owner grain so it is re-activated after a failover.</summary>
    internal static readonly TimeSpan PingPeriod = TimeSpan.FromSeconds(30);

    /// <summary>How old a snapshot may get before it publishes nothing.</summary>
    internal static readonly TimeSpan StalenessThreshold = TimeSpan.FromSeconds(90);

    /// <summary>How long an emptied bucket keeps reporting zero before the series is retired.</summary>
    internal static readonly TimeSpan ZeroRetention = TimeSpan.FromMinutes(5);
}
