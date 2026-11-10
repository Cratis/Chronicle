// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_VerdictResult;

/// <summary>
/// Specifies the repository's clean and defect exit-code contract.
/// </summary>
public class when_mapping_exit_codes : for_Verdict.given.a_snapshot
{
    [Fact] void should_exit_zero_for_a_complete_clean_snapshot() => Verdict.Compare(Input).ExitCode.ShouldEqual(0);
    [Fact] void should_exit_one_for_the_self_test_missing_event() => Verdict.Compare(Input with { Events = [] }).ExitCode.ShouldEqual(1);
    [Fact] void should_exit_one_for_a_drain_violation() => Verdict.Compare(Input with { Settled = false }).ExitCode.ShouldEqual(1);
}
