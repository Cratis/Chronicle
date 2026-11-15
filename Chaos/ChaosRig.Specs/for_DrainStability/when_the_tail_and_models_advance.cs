// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DrainStability;

/// <summary>
/// Specifies that durable tail/model changes restart the window, but enumeration order does not.
/// </summary>
public class when_the_tail_and_models_advance : for_Verdict.given.a_snapshot
{
    bool _afterAdvance;
    bool _afterReordering;
    bool _thirdAtPosition;

    void Because()
    {
        var stability = new DrainStability();
        stability.Observe(Input);
        stability.Observe(Input);
        var advanced = Input with
        {
            Ledger = [Input.Ledger[0], new("two", "other", 2, "acked")],
            Events = [Input.Events[0], new("two", "other", 1, 2)],
            Observers = [new("observer", 1, true)],
            Projections = [Input.Projections[0], new("other", 1, 2)],
            Reductions = [Input.Reductions[0], new("other", 1, 2)],
            SideLedger = ["one", "two"]
        };
        _afterAdvance = stability.Observe(advanced);
        _afterReordering = stability.Observe(advanced with
        {
            Events = advanced.Events.Reverse().ToArray(),
            Projections = advanced.Projections.Reverse().ToArray(),
            Reductions = advanced.Reductions.Reverse().ToArray()
        });
        _thirdAtPosition = stability.Observe(advanced);
    }

    [Fact] void should_restart_the_window_at_the_new_tail_and_models() => _afterAdvance.ShouldBeFalse();
    [Fact] void should_not_accept_only_two_samples_at_the_new_tail() => _afterReordering.ShouldBeFalse();
    [Fact] void should_ignore_enumeration_order_in_durable_progress() => _thirdAtPosition.ShouldBeTrue();
}
