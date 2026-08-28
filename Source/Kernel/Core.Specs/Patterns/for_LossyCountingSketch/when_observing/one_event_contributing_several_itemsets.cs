// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_observing;

/// <summary>
/// Support is a share of events, so an event that contributes several itemsets is still one observation. Counting
/// each itemset as an observation would divide every frequency by a denominator many times too large and nothing
/// would ever clear the support threshold.
/// </summary>
public class one_event_contributing_several_itemsets : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

    LossyCountingSketch _sketch;

    void Establish() => _sketch = new(0.01d, 1d);

    void Because() => _sketch.Observe(
    [
        new FacetSet([new Facet(FacetName.Day, "Monday")]),
        new FacetSet([new Facet(FacetName.TimeBucket, "Morning")]),
        new FacetSet([new Facet(FacetName.Day, "Monday"), new Facet(FacetName.TimeBucket, "Morning")])
    ],
    _occurred);

    [Fact] void should_count_a_single_observation() => _sketch.Observed.ShouldEqual(1L);
    [Fact] void should_retain_every_itemset() => _sketch.Count.ShouldEqual(3);
}
