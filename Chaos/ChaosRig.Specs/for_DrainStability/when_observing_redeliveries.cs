// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Chronicle.Chaos.for_DrainStability;

/// <summary>
/// Specifies that growing at-least-once delivery counts do not prevent durable stability.
/// </summary>
public class when_observing_redeliveries : for_Verdict.given.a_snapshot
{
    bool _first;
    bool _second;
    bool _third;

    void Because()
    {
        var stability = new DrainStability();
        _first = stability.Observe(Input);
        _second = stability.Observe(Input with { SideLedger = ["one", "one"] });
        _third = stability.Observe(Input with { SideLedger = Enumerable.Repeat("one", 81).ToArray() });
    }

    [Fact] void should_not_accept_one_sample() => _first.ShouldBeFalse();
    [Fact] void should_not_accept_two_samples() => _second.ShouldBeFalse();
    [Fact] void should_accept_three_unchanged_durable_samples_despite_eighty_redeliveries() => _third.ShouldBeTrue();
}
