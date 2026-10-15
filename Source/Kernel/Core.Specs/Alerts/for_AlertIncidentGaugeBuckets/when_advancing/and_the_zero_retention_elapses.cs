// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;

namespace Cratis.Chronicle.Alerts.for_AlertIncidentGaugeBuckets.when_advancing;

public class and_the_zero_retention_elapses : given.counts
{
    AlertIncidentGaugeBuckets _result;

    void Establish() => _result = AlertIncidentGaugeBuckets.Empty.Next([Count(AlertSeverity.Warning, 1)], _now, _retention).Next([], _now, _retention);

    void Because() => _result = _result.Next([], _now + _retention, _retention);

    [Fact] void should_retire_the_series() => _result.Values.ShouldBeEmpty();
}
