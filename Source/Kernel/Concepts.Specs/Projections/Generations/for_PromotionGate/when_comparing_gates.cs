// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Concepts.Projections.Generations.for_PromotionGate;

public class when_comparing_gates : Specification
{
    PromotionGate _gate;
    DateTimeOffset _scheduledAt;
    SemanticVersion _requiredVersion;

    void Establish()
    {
        _scheduledAt = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        _requiredVersion = new(1, 2, 3);
    }

    void Because() => _gate = new(PromotionGateMode.Deployment, _scheduledAt, _requiredVersion, true);

    [Fact] void should_equal_a_gate_with_the_same_conditions() => _gate.ShouldEqual(new PromotionGate(PromotionGateMode.Deployment, _scheduledAt, new(1, 2, 3), true));
    [Fact] void should_distinguish_a_different_mode() => _gate.ShouldNotEqual(_gate with { Mode = PromotionGateMode.Operator });
    [Fact] void should_distinguish_a_different_schedule() => _gate.ShouldNotEqual(_gate with { ScheduledAt = null });
    [Fact] void should_distinguish_a_different_required_version() => _gate.ShouldNotEqual(_gate with { RequiredVersion = null });
    [Fact] void should_distinguish_a_sticky_gate() => _gate.ShouldNotEqual(_gate with { IsSticky = false });
}
