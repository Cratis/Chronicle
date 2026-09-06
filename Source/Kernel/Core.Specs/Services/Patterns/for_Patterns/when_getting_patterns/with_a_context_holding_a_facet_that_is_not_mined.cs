// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_patterns;

/// <summary>
/// A caller can describe its situation in whatever terms it has. Facets the store does not mine are discarded
/// rather than narrowing the lookup to nothing - the question is still answerable from the parts that are mined.
/// </summary>
public class with_a_context_holding_a_facet_that_is_not_mined : given.a_patterns_service
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
            { FacetName.CorrelationRootId.Value, "correlation-1" },
            { "SomethingNobodyMines", "whatever" }
        }
    });

    [Fact] void should_still_answer_from_the_mined_facets() => _result.Count().ShouldEqual(1);
    [Fact] void should_answer_with_the_pattern_for_the_mined_facet() => _result.Single().Facets[FacetName.Day.Value].ShouldEqual("Monday");
}
