// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.when_advancing;

public class and_a_bucket_disappears : given.counts
{
    AlertIncidentGaugeBuckets _result;

    void Establish() => _result = AlertIncidentGaugeBuckets.Empty.Next([Count(AlertSeverity.Warning, 1)], _now, _retention);

    void Because() => _result = _result.Next([], _now.AddSeconds(30), _retention);

    [Fact] void should_report_zero() => ValueOf(_result, AlertSeverity.Warning).ShouldEqual(0L);
    [Fact] void should_retire_after_the_retention() => _result.RetireAt.Single().Value.ShouldEqual(_now.AddSeconds(30) + _retention);
}
