// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DrainStability;

/// <summary>
/// Specifies that a collection failure cannot count toward a complete observation window.
/// </summary>
public class when_collection_breaks_the_window : for_Verdict.given.a_snapshot
{
    bool _firstAfterFailure;
    bool _secondAfterFailure;
    bool _thirdAfterFailure;

    void Because()
    {
        var stability = new DrainStability();
        stability.Observe(Input);
        stability.Observe(Input);
        stability.Reset();
        _firstAfterFailure = stability.Observe(Input);
        _secondAfterFailure = stability.Observe(Input);
        _thirdAfterFailure = stability.Observe(Input);
    }

    [Fact] void should_discard_the_old_window() => _firstAfterFailure.ShouldBeFalse();
    [Fact] void should_require_a_new_third_sample() => _secondAfterFailure.ShouldBeFalse();
    [Fact] void should_accept_a_complete_new_window() => _thirdAfterFailure.ShouldBeTrue();
}
