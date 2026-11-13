// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DrainStability;

/// <summary>
/// Specifies that an actual liveness finding breaks the stable drain window.
/// </summary>
public class when_failed_partitions_interrupt_the_window : for_Verdict.given.a_snapshot
{
    bool _failed;
    bool _firstAfterRecovery;
    bool _secondAfterRecovery;
    bool _thirdAfterRecovery;

    void Because()
    {
        var stability = new DrainStability();
        stability.Observe(Input);
        stability.Observe(Input);
        _failed = stability.Observe(Input with { FailedPartitions = 1 });
        _firstAfterRecovery = stability.Observe(Input);
        _secondAfterRecovery = stability.Observe(Input);
        _thirdAfterRecovery = stability.Observe(Input);
    }

    [Fact] void should_not_accept_failed_partitions() => _failed.ShouldBeFalse();
    [Fact] void should_not_reuse_samples_from_before_the_finding() => _firstAfterRecovery.ShouldBeFalse();
    [Fact] void should_require_three_recovered_samples() => _secondAfterRecovery.ShouldBeFalse();
    [Fact] void should_accept_a_recovered_stable_window() => _thirdAfterRecovery.ShouldBeTrue();
}
