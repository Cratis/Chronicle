// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_patterns;

public class with_a_confidence_higher_than_anything_held : given.a_patterns_service
{
    IEnumerable<Pattern> _result;

    async Task Because() => _result = await _service.GetPatterns(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespaceName.Default,
        GroupingKey = Scope,
        Context = new Dictionary<string, string> { { FacetName.Day.Value, "Monday" } },
        MinimumConfidence = 0.95d
    });

    [Fact] void should_return_nothing_rather_than_the_best_of_a_bad_set() => _result.ShouldBeEmpty();
}
