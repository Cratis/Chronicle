// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;
using Cratis.Metrics;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>Records the silo-wide number of retained projection pipelines.</summary>
/// <param name="meter">The producer's meter.</param>
/// <param name="names">The selected metric naming mode.</param>
internal sealed class ProjectionPipelineManagerMetrics(IMeter<ProjectionPipelineManager>? meter, MetricNames names)
{
    readonly MetricInstrument<int, GaugeMetricRecorder<int>> _count = new MetricSet(meter?.ActualMeter, names).ConventionOnlyGauge<int>(
        WellKnownMetricNames.ProjectionPipelines,
        WellKnownMetricNames.ProjectionPipelinesUnit,
        "Number of projection pipelines retained by the kernel");

    /// <summary>Records the current number of retained pipelines.</summary>
    /// <param name="count">The number of pipelines.</param>
    internal void Pipelines(int count) => _count.Record(count);
}
