// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineMath;

public class when_reporting_censored_markers : Specification
{
    JsonElement _report;
    void Because() => _report = JsonSerializer.SerializeToElement(RebuildBaselineMath.Latencies([1, 2, 3], new Dictionary<int, double> { [0] = 50, [1] = 100 }, 4, true));
    [Fact] void should_exclude_unobserved_markers_from_percentiles() => _report.GetProperty("P50Ms").GetDouble().ShouldEqual(50d);
    [Fact] void should_report_p99_of_observed_markers() => _report.GetProperty("P99Ms").GetDouble().ShouldEqual(100d);
    [Fact] void should_report_a_lower_bound_for_the_censored_marker() => _report.GetProperty("Censored")[0].GetProperty("ElapsedLowerBoundMs").GetDouble().ShouldEqual(1000d);
    [Fact] void should_count_timed_out_markers() => _report.GetProperty("TimedOut").GetInt32().ShouldEqual(1);
    [Fact] void should_not_report_zero_for_no_observations() => RebuildBaselineMath.Percentile([], .99).ShouldBeNull();
}
