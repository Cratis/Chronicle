// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ScalingReport;

public class when_sizes_grow_faster_than_linearly : Specification
{
    string _report = default!;
    void Because() => _report = ScalingReport.Format([("sqlite/catch-up-full", 1000, 10), ("sqlite/catch-up-full", 2000, 30)]);
    [Fact] void should_show_normalized_growth() => _report.ShouldContain("| 2000 | 30 | 15 | 1.5 | super-linear |");
}
