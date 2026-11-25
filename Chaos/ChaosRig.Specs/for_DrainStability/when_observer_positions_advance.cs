// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DrainStability;

/// <summary>
/// Specifies actual position equality rather than merely consecutive clean verdicts.
/// </summary>
public class when_observer_positions_advance : for_Verdict.given.a_snapshot
{
    bool _afterAdvance;
    bool _secondAtPosition;
    bool _thirdAtPosition;

    void Because()
    {
        var stability = new DrainStability();
        stability.Observe(Input);
        stability.Observe(Input);
        var advanced = Input with { Observers = [new("observer", 1, true)] };
        _afterAdvance = stability.Observe(advanced);
        _secondAtPosition = stability.Observe(advanced);
        _thirdAtPosition = stability.Observe(advanced);
    }

    [Fact] void should_restart_the_window_even_when_both_positions_have_caught_up() => _afterAdvance.ShouldBeFalse();
    [Fact] void should_require_a_third_sample_at_the_new_position() => _secondAtPosition.ShouldBeFalse();
    [Fact] void should_accept_three_equal_durable_positions() => _thirdAtPosition.ShouldBeTrue();
}
