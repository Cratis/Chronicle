// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_removing;

/// <summary>
/// A scope whose behavior has all decayed below the threshold survives with nothing, and an empty surviving set
/// has to mean exactly that. Reading it as "nothing to remove" would leave stale behavior on record forever.
/// </summary>
public class everything_because_nothing_survived : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _remaining;
    IEnumerable<PatternGroupingKey> _scopes;

    async Task Because()
    {
        await _storage.RemoveAllExcept("user-42", []);
        _remaining = await _storage.GetForScope("user-42");
        _scopes = await _storage.GetScopes();
    }

    [Fact] void should_remove_everything_for_the_scope() => _remaining.ShouldBeEmpty();
    [Fact] void should_leave_the_other_scope_listed() => _scopes.ShouldContain(new PatternGroupingKey("user-7"));
}
