// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ScalingReport;

public class when_scaling_is_linear : Specification
{
    string _report = default!;
    void Because() => _report = ScalingReport.Format([("sqlite/catch-up-full", 1000, 10), ("sqlite/catch-up-full", 2000, 20)]);
    [Fact] void should_show_linear_growth() => _report.ShouldContain("| 2000 | 20 | 10 | 1 |  |");
    [Fact] void should_not_flag_growth() => _report.ShouldNotContain("super-linear");
}
