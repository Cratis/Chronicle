// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Telemetry.when_getting_the_metric_series_limit;

public class and_it_is_configured : Specification
{
    int _limit;

    void Because()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Telemetry:MetricSeriesLimit"] = "20000" })
            .Build();
        _limit = configuration.GetSection("Telemetry").Get<Telemetry>()!.EffectiveMetricSeriesLimit;
    }

    [Fact] void should_use_the_configured_limit() => _limit.ShouldEqual(20000);
}
