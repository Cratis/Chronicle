// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_removing;

public class everything_except_the_surviving_patterns : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _remaining;
    IEnumerable<BehaviorPattern> _otherScope;

    async Task Because()
    {
        await _storage.RemoveAllExcept("user-42", [_mondayMorning.Facets.Key]);
        _remaining = await _storage.GetForScope("user-42");
        _otherScope = await _storage.GetForScope("user-7");
    }

    [Fact] void should_keep_the_surviving_pattern() => _remaining.ShouldContainOnly(_mondayMorning);
    [Fact] void should_leave_another_scope_alone() => _otherScope.ShouldContainOnly(_forSomebodyElse);
}
