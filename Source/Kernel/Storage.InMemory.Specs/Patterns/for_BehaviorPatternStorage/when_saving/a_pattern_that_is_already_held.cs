// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_saving;

/// <summary>
/// Mining reports what a scope's behavior currently is, not what changed. Saving is therefore a replace: a scope
/// that keeps doing the same thing must not accumulate a row per flush.
/// </summary>
public class a_pattern_that_is_already_held : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _result;

    async Task Because()
    {
        await _storage.Save([Pattern("user-42", [new Facet(FacetName.Day, "Monday")], confidence: 0.42d)]);
        _result = await _storage.GetForScope("user-42");
    }

    [Fact] void should_not_add_a_second_row_for_it() => _result.Count().ShouldEqual(2);
    [Fact] void should_hold_the_new_confidence() =>
        _result.Single(_ => _.Specificity == 1).Confidence.Value.ShouldEqual(0.42d);
}
