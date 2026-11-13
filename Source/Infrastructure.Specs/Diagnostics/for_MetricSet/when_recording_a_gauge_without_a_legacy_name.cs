// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_MetricSet;

public class when_recording_a_gauge_without_a_legacy_name : given.a_metric_listener
{
    void Because()
    {
        foreach (var names in new[] { MetricNames.Legacy, MetricNames.Convention, MetricNames.Both })
        {
            new MetricSet(_meter, names).ConventionOnlyGauge<int>(WellKnownMetricNames.ProjectionCachedGraphs, WellKnownMetricNames.ProjectionCachedGraphsUnit, "Cached graphs").Record(256);
        }
    }

    [Fact] void should_publish_the_convention_name_in_every_mode() => _measurements.Select(measurement => measurement.Name).ShouldEqual(Enumerable.Repeat(WellKnownMetricNames.ProjectionCachedGraphs, 3));
    [Fact] void should_record_once_per_mode() => _measurements.Select(measurement => measurement.Value).ShouldEqual([256d, 256d, 256d]);
    [Fact] void should_use_the_graph_unit() => _instruments.TrueForAll(instrument => instrument.Unit == WellKnownMetricNames.ProjectionCachedGraphsUnit).ShouldBeTrue();
}
