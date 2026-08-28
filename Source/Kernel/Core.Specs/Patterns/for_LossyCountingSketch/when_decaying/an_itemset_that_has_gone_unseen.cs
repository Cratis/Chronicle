// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_decaying;

public class an_itemset_that_has_gone_unseen : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

    LossyCountingSketch _sketch;
    FacetSet _itemset;
    double _weightBefore;
    double _weightAfter;

    void Establish()
    {
        _sketch = new(0.01d, 0.5d);
        _itemset = new FacetSet([new Facet(FacetName.Day, "Monday")]);
        _sketch.Observe([_itemset], _occurred);
        _weightBefore = _sketch.Entries.Single().Weight;
    }

    void Because()
    {
        _sketch.Decay(_occurred.AddDays(3));
        _weightAfter = _sketch.Entries.Single().Weight;
    }

    [Fact] void should_start_at_a_full_weight() => _weightBefore.ShouldEqual(1d);
    [Fact] void should_halve_the_weight_for_every_day_unseen() => Math.Round(_weightAfter, 6).ShouldEqual(0.125d);
    [Fact] void should_not_change_when_it_was_last_observed() => _sketch.Entries.Single().LastSeen.ShouldEqual(_occurred);
    [Fact] void should_not_change_how_often_it_occurred() => _sketch.Entries.Single().Frequency.ShouldEqual(1L);
}
