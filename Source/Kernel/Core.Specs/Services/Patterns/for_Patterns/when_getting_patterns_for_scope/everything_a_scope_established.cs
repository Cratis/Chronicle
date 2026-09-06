// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Patterns;
using Pattern = Cratis.Chronicle.Contracts.Patterns.Pattern;

namespace Cratis.Chronicle.Services.Patterns.for_Patterns.when_getting_patterns_for_scope;

/// <summary>
/// Browsing a scope is a different question from asking about a situation: it lists what the scope established,
/// unfiltered, so a view can show weak patterns alongside strong ones.
/// </summary>
public class everything_a_scope_established : given.a_patterns_service
{
    IEnumerable<Pattern> _result;

    async Task Because() => _result = await _service.GetPatternsForScope(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespaceName.Default,
        GroupingKey = Scope
    });

    [Fact] void should_return_every_pattern_held() => _result.Count().ShouldEqual(5);
    [Fact] void should_include_the_ones_below_the_confidence_threshold() => _result.Any(_ => _.Confidence < 0.5d).ShouldBeTrue();
    [Fact] void should_include_the_ones_naming_an_action() => _result.Any(_ => _.Facets.ContainsKey(FacetName.CommandType.Value)).ShouldBeTrue();
}
