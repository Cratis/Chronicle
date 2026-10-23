// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineProbe;

public class when_sampling_only_at_the_final_fence : Specification
{
    RebuildBaselineProbe _probe = null!;
    JsonElement _report;

    void Establish() => _probe = new RebuildBaselineProbe(() => Task.FromResult(1), Stopwatch.StartNew(), new ConcurrentQueue<double>([0d]));

    async Task Because()
    {
        await _probe.Sample(recordObserved: false);
        _report = JsonSerializer.SerializeToElement(_probe.Report(false));
    }

    [Fact] void should_not_count_the_final_sample_as_observed() => _report.GetProperty("Observed").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_the_marker_censored() => _report.GetProperty("Censored").GetArrayLength().ShouldEqual(1);
    [Fact] void should_leave_observed_latency_percentiles_null() => _report.GetProperty("P99Ms").ValueKind.ShouldEqual(JsonValueKind.Null);
}
