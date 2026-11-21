// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_PlanReport;

public class when_a_plan_failed : Specification
{
    string _report = default!;
    void Because() => _report = PlanReport.Format("pg", 1000, [("tail", "SELECT max(id) FROM events", "not captured: explain unavailable")]);
    [Fact] void should_keep_the_failure_as_text() => _report.ShouldContain("not captured: explain unavailable");
    [Fact] void should_not_invent_an_index() => _report.ShouldContain("Indexes used: none reported");
}
