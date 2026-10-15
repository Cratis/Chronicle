// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_observing;

public class and_the_owner_cleared : given.a_gauge
{
    void Establish()
    {
        _holder.Activate(this);
        _holder.Publish(this, Snapshot());
        _holder.Clear(this);
    }

    [Fact] void should_publish_nothing() => _gauge.Observe().ShouldBeEmpty();
    [Fact] void should_not_report_availability() => _gauge.ObserveAvailable().ShouldBeEmpty();
}
