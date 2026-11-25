// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>Records the silo-wide number of retained compiled projection graphs.</summary>
/// <param name="meter">The producer's meter.</param>
/// <param name="names">The selected metric naming mode.</param>
internal sealed class ProjectionsManagerMetrics(IMeter<ProjectionsManager>? meter, MetricNames names)
{
    readonly MetricInstrument<int, GaugeMetricRecorder<int>> _count = new MetricSet(meter?.ActualMeter, names).ConventionOnlyGauge<int>(
        WellKnownMetricNames.ProjectionCachedGraphs,
        WellKnownMetricNames.ProjectionCachedGraphsUnit,
        "Number of compiled projection graphs retained in the engine cache");

    /// <summary>Records the current number of cached compiled root graphs.</summary>
    /// <param name="count">The number of cached graphs.</param>
    internal void CachedGraphs(int count) => _count.Record(count);
}
