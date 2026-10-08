// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.when_advancing;

public class and_a_retiring_bucket_reopens : given.counts
{
    AlertIncidentGaugeBuckets _result;

    void Establish() => _result = AlertIncidentGaugeBuckets.Empty.Next([Count(AlertSeverity.Warning, 1)], _now, _retention).Next([], _now, _retention);

    void Because() => _result = _result.Next([Count(AlertSeverity.Warning, 3)], _now.AddSeconds(30), _retention);

    [Fact] void should_report_the_count() => ValueOf(_result, AlertSeverity.Warning).ShouldEqual(3L);
    [Fact] void should_no_longer_retire_it() => _result.RetireAt.ShouldBeEmpty();
    [Fact] void should_publish_it_once() => _result.Values.Count().ShouldEqual(1);
}
