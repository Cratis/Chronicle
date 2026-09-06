// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_observing;

public class the_same_itemset_repeatedly : Specification
{
    static readonly DateTimeOffset _occurred = new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

    LossyCountingSketch _sketch;
    FacetSet _itemset;

    void Establish()
    {
        _sketch = new(0.01d, 1d);
        _itemset = new FacetSet([new Facet(FacetName.Day, "Monday")]);
    }

    void Because()
    {
        for (var count = 0; count < 5; count++)
        {
            _sketch.Observe([_itemset], _occurred);
        }
    }

    [Fact] void should_count_one_observation_per_event() => _sketch.Observed.ShouldEqual(5L);
    [Fact] void should_count_the_itemset_once_per_observation() => _sketch.GetFrequency(_itemset.Key).ShouldEqual(5L);
    [Fact] void should_retain_only_the_one_itemset() => _sketch.Count.ShouldEqual(1);
    [Fact] void should_have_no_error_for_an_itemset_that_was_there_from_the_start() => _sketch.Entries.Single().Error.ShouldEqual(0L);
}
