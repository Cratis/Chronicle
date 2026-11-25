// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_ScalingReport;

public class when_only_one_size_ran : Specification
{
    string _report = default!;
    void Because() => _report = ScalingReport.Format([("sqlite/catch-up-full", 1000, 10)]);
    [Fact] void should_not_show_a_scaling_table() => _report.ShouldEqual(string.Empty);
}
