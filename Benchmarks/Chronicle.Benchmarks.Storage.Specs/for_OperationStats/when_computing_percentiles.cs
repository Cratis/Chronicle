// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_OperationStats;

public class when_computing_percentiles : Specification
{
    double _p50;
    double _p99;
    double _empty;

    void Because()
    {
        double[] samples = [.. Enumerable.Range(1, 100).Select(_ => (double)_)];
        _p50 = OperationStats.Percentile(samples, 50);
        _p99 = OperationStats.Percentile(samples, 99);
        _empty = OperationStats.Percentile([], 99);
    }

    [Fact] void should_find_the_median() => _p50.ShouldEqual(50);
    [Fact] void should_find_the_99th_percentile() => _p99.ShouldEqual(99);
    [Fact] void should_be_zero_without_samples() => _empty.ShouldEqual(0);
}
