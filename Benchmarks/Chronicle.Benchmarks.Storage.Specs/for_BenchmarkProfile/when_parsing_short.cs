// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkProfile;

public class when_parsing_short : Specification
{
    BenchmarkProfile _profile = default!;

    void Because() => _profile = BenchmarkProfile.Parse("short");

    [Fact] void should_scale_catch_up_across_four_sizes() => _profile.CatchUpCounts.ShouldEqual<int>(2_500, 5_000, 10_000, 20_000);
    [Fact] void should_name_the_profile() => _profile.Name.ShouldEqual("short");
    [Fact] void should_seed_ten_thousand_events() => _profile.EventCounts.ShouldEqual<int>(10_000);
    [Fact] void should_include_thirty_two_writers() => _profile.Concurrency.ShouldEqual<int>(1, 8, 32);
    [Fact] void should_keep_the_batch_sizes() => _profile.BatchSizes.ShouldEqual<int>(10, 100, 1_000);
    [Fact] void should_keep_the_constraint_batch_sizes() => _profile.ConstraintBatchSizes.ShouldEqual<int>(1, 50, 500);
    [Fact] void should_use_256_byte_payloads() => _profile.PayloadBytes.ShouldEqual(256);
    [Fact] void should_warm_up_twice() => _profile.WarmupCount.ShouldEqual(2);
    [Fact] void should_measure_ten_iterations() => _profile.IterationCount.ShouldEqual(10);
    [Fact] void should_budget_two_hours() => _profile.TimeoutMinutes.ShouldEqual(120);
    [Fact] void should_not_require_confirmation() => _profile.RequiresConfirmation.ShouldBeFalse();
}
