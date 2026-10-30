// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineMath;

public class when_calculating_fence_throughput : Specification
{
    double _rate;
    void Because() => _rate = RebuildBaselineMath.Throughput(100, 5, 7);
    [Fact] void should_use_request_to_coverage_interval() => _rate.ShouldEqual(50d);
}
