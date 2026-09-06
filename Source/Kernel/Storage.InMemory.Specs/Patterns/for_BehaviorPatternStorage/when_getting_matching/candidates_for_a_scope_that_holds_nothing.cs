// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_getting_matching;

public class candidates_for_a_scope_that_holds_nothing : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _matching;
    IEnumerable<BehaviorPattern> _all;

    async Task Because()
    {
        _matching = await _storage.GetMatching("user-nobody", [_monday.Facets.Key]);
        _all = await _storage.GetForScope("user-nobody");
    }

    [Fact] void should_return_nothing_for_a_lookup() => _matching.ShouldBeEmpty();
    [Fact] void should_return_nothing_for_the_whole_scope() => _all.ShouldBeEmpty();
}
