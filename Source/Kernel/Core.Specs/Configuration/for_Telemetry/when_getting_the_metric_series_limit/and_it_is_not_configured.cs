// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Telemetry.when_getting_the_metric_series_limit;

public class and_it_is_not_configured : Specification
{
    int _limit;

    void Because() => _limit = new Telemetry().EffectiveMetricSeriesLimit;

    [Fact] void should_keep_the_default_of_500() => _limit.ShouldEqual(500);
}
