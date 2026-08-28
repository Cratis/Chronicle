// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_patterns;

public class for_a_partial_context : given.a_patterns_service
{
    IEnumerable<Pattern> _result;

    async Task Because() => _result = await _service.GetPatterns(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespaceName.Default,
        GroupingKey = Scope,
        Context = new Dictionary<string, string>
        {
            { FacetName.Day.Value, "Monday" },
            { FacetName.TimeBucket.Value, "Morning" }
        }
    });

    [Fact] void should_return_the_patterns_clearing_the_configured_threshold() => _result.Count().ShouldEqual(2);
    [Fact] void should_rank_the_most_specific_first() => _result.First().Facets.Count.ShouldEqual(2);
    [Fact] void should_carry_the_facets_the_pattern_constrains() => _result.First().Facets[FacetName.Day.Value].ShouldEqual("Monday");
    [Fact] void should_carry_the_confidence() => _result.First().Confidence.ShouldEqual(0.9d);
    [Fact] void should_carry_the_occurrences() => _result.First().Occurrences.ShouldEqual(10L);
    [Fact] void should_carry_the_weight() => _result.First().Weight.ShouldEqual(1d);
    [Fact] void should_carry_the_scope() => _result.First().GroupingKey.ShouldEqual(Scope);
    [Fact] void should_not_return_a_pattern_below_the_threshold() => _result.Any(_ => _.Confidence < 0.5d).ShouldBeFalse();
}
