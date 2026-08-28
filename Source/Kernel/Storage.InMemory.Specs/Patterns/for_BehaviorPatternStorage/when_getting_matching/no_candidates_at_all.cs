// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;

namespace Cratis.Chronicle.Storage.InMemory.Patterns.for_BehaviorPatternStorage.when_getting_matching;

/// <summary>
/// An empty candidate set asks for nothing, and must therefore return nothing. Treating it as "do not narrow" -
/// the way a query criteria sentinel would - would hand back everything the scope has ever done in answer to a
/// question about no context at all.
/// </summary>
public class no_candidates_at_all : given.a_storage_with_patterns_for_two_scopes
{
    IEnumerable<BehaviorPattern> _result;

    async Task Because() => _result = await _storage.GetMatching("user-42", []);

    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();
}
