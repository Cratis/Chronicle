// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.when_advancing;

public class and_incidents_open_for_the_first_time : given.counts
{
    AlertIncidentGaugeBuckets _result;

    void Because() => _result = AlertIncidentGaugeBuckets.Empty.Next([Count(AlertSeverity.Warning, 2), Count(AlertSeverity.Critical, 0)], _now, _retention);

    [Fact] void should_publish_the_open_bucket() => ValueOf(_result, AlertSeverity.Warning).ShouldEqual(2L);
    [Fact] void should_not_publish_empty_buckets() => _result.Values.Count().ShouldEqual(1);
}
