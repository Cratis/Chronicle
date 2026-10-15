// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_observing;

public class and_the_snapshot_is_fresh : given.a_gauge
{
    void Establish()
    {
        _holder.Activate(this);
        _holder.Publish(this, Snapshot(4));
        _time.Advance(AlertIncidentsGaugeTiming.StalenessThreshold);
    }

    [Fact] void should_publish_the_measurement() => _gauge.Observe().Single().Value.ShouldEqual(4L);
    [Fact] void should_tag_with_exactly_the_six_keys() => _gauge.Observe().Single().Tags.ToArray().Select(_ => _.Key)
        .ShouldContainOnly("EventStore", "Namespace", "ObserverId", "EventSequenceId", "Condition", "Severity");
    [Fact] void should_be_available() => _gauge.ObserveAvailable().Single().Value.ShouldEqual(1);
}
