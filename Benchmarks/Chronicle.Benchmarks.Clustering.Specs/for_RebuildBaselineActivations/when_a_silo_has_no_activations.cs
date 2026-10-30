// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Clustering.for_RebuildBaselineActivations;

public class when_a_silo_has_no_activations : Specification
{
    RebuildBaselineActivations _report = null!;
    void Because() => _report = RebuildBaselineActivations.From([]);
    [Fact] void should_report_zero_total() => _report.TotalActivations.ShouldEqual(0L);
    [Fact] void should_report_no_grain_types() => _report.ActivationsByGrainType.ShouldBeEmpty();
}
