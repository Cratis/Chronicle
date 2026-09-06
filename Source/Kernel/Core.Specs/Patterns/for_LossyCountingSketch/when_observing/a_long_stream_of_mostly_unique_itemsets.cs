// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_observing;

/// <summary>
/// The whole point of the sketch is that memory does not grow with the stream. Ten thousand one-off itemsets and
/// one recurring one must leave the recurring one standing and almost nothing else - if the sketch simply
/// remembered everything, it would be a dictionary with extra steps and would not survive a real event store.
/// </summary>
public class a_long_stream_of_mostly_unique_itemsets : Specification
{
    const int Observations = 10_000;

    static readonly DateTimeOffset _occurred = new(2026, 8, 24, 9, 0, 0, TimeSpan.Zero);

    LossyCountingSketch _sketch;
    FacetSet _recurring;

    void Establish()
    {
        _sketch = new(0.001d, 1d);
        _recurring = new FacetSet([new Facet(FacetName.CommandType, "ApproveExpenseReport")]);
    }

    void Because()
    {
        for (var count = 0; count < Observations; count++)
        {
            var oneOff = new FacetSet([new Facet(FacetName.CorrelationRootId, count.ToString(CultureInfo.InvariantCulture))]);
            _sketch.Observe([_recurring, oneOff], _occurred);
        }
    }

    [Fact] void should_have_seen_every_observation() => _sketch.Observed.ShouldEqual(Observations);
    [Fact] void should_keep_the_recurring_itemset() => _sketch.GetFrequency(_recurring.Key).ShouldEqual(Observations);
    [Fact] void should_not_grow_with_the_stream() => _sketch.Count.ShouldBeLessThan(Observations / 2);
}
