// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_observing;

public class and_no_snapshot_was_published : given.a_gauge
{
    [Fact] void should_publish_nothing() => _gauge.Observe().ShouldBeEmpty();
    [Fact] void should_not_report_availability() => _gauge.ObserveAvailable().ShouldBeEmpty();
}
