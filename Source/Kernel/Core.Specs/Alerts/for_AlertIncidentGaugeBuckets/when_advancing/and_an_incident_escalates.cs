// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.when_advancing;

public class and_an_incident_escalates : given.counts
{
    AlertIncidentGaugeBuckets _result;

    void Establish() => _result = AlertIncidentGaugeBuckets.Empty.Next([Count(AlertSeverity.Warning, 1)], _now, _retention);

    void Because() => _result = _result.Next([Count(AlertSeverity.Critical, 1)], _now.AddSeconds(30), _retention);

    [Fact] void should_report_zero_for_the_old_bucket() => ValueOf(_result, AlertSeverity.Warning).ShouldEqual(0L);
    [Fact] void should_report_the_new_bucket() => ValueOf(_result, AlertSeverity.Critical).ShouldEqual(1L);
    [Fact] void should_preserve_the_total() => _result.Values.Sum(_ => _.Value).ShouldEqual(1L);
}
