// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_observing;

public class and_the_snapshot_is_stale : given.a_gauge
{
    void Establish()
    {
        _holder.Activate(this);
        _holder.Publish(this, Snapshot());
        _time.Advance(AlertIncidentsGaugeTiming.StalenessThreshold + TimeSpan.FromSeconds(1));
    }

    [Fact] void should_publish_nothing() => _gauge.Observe().ShouldBeEmpty();
    [Fact] void should_report_unavailable() => _gauge.ObserveAvailable().Single().Value.ShouldEqual(0);
}
