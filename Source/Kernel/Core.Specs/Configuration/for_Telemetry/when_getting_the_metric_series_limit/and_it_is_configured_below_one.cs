// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_Telemetry.when_getting_the_metric_series_limit;

public class and_it_is_configured_below_one : Specification
{
    int _limit;

    void Because() => _limit = new Telemetry { MetricSeriesLimit = 0 }.EffectiveMetricSeriesLimit;

    [Fact] void should_use_the_default() => _limit.ShouldEqual(Telemetry.DefaultMetricSeriesLimit);
}
