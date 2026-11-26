// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_BenchmarkProfile;

public class when_parsing_smoke : Specification
{
    BenchmarkProfile _profile = default!;
    void Because() => _profile = BenchmarkProfile.Parse("smoke");
    [Fact] void should_scale_catch_up_cheaply() => _profile.CatchUpCounts.ShouldEqual<int>(500, 1_000, 2_000);
    [Fact] void should_keep_activation_sizes() => _profile.EventCounts.ShouldEqual<int>(1_000);
}
