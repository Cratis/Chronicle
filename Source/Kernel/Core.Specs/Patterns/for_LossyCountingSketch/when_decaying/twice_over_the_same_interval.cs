// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_decaying;

/// <summary>
/// The background decay pass runs on a schedule, and a schedule fires more than once. Decaying to a moment the
/// weight is already decayed to must be a no-op, or how fast behavior is forgotten would depend on how often the
/// pass happens to run rather than on the configured factor.
/// </summary>
public class twice_over_the_same_interval : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

    LossyCountingSketch _sketch;
    double _afterFirstPass;
    double _afterSecondPass;

    void Establish()
    {
        _sketch = new(0.01d, 0.5d);
        _sketch.Observe([new FacetSet([new Facet(FacetName.Day, "Monday")])], _occurred);
    }

    void Because()
    {
        _sketch.Decay(_occurred.AddDays(2));
        _afterFirstPass = _sketch.Entries.Single().Weight;

        _sketch.Decay(_occurred.AddDays(2));
        _afterSecondPass = _sketch.Entries.Single().Weight;
    }

    [Fact] void should_decay_on_the_first_pass() => Math.Round(_afterFirstPass, 6).ShouldEqual(0.25d);
    [Fact] void should_not_decay_again_on_the_second() => _afterSecondPass.ShouldEqual(_afterFirstPass);
}
