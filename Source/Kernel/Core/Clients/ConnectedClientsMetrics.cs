// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Records connected clients with the naming mode selected at construction.
/// </summary>
/// <param name="meter">The producer's meter.</param>
/// <param name="names">The naming mode.</param>
internal sealed class ConnectedClientsMetrics(IMeter<ConnectedClients> meter, MetricNames names)
{
    readonly MetricInstrument<int, GaugeMetricRecorder<int>> _count = new MetricSet(meter.ActualMeter, names).Gauge<int>(
        WellKnownMetricNames.ConnectedClientCount,
        WellKnownMetricNames.ConnectedClientCountUnit,
        "Number of connected clients");

    /// <summary>Records the current number of connected clients.</summary>
    /// <param name="count">The count.</param>
    internal void ConnectedClients(int count) => _count.Record(count);
}
