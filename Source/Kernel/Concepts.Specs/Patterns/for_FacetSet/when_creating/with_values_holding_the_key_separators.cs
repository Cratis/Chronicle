// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Patterns.for_FacetSet.when_creating;

/// <summary>
/// Two sets whose values differ only in where the separators fall must not produce the same key - the key is what
/// a pattern is counted and stored by, so a collision would silently merge two unrelated behaviors into one.
/// </summary>
public class with_values_holding_the_key_separators : Specification
{
    FacetSet _first;
    FacetSet _second;

    void Because()
    {
        _first = new FacetSet([new Facet(FacetName.CommandType, "a=b"), new Facet(FacetName.Day, "c")]);
        _second = new FacetSet([new Facet(FacetName.CommandType, "a"), new Facet(FacetName.Day, "b=c")]);
    }

    [Fact] void should_escape_the_separator_in_the_key() => _first.Key.Value.ShouldEqual(@"CommandType=a\=b;Day=c");
    [Fact] void should_not_produce_the_same_key_for_the_other_set() => _second.Key.ShouldNotEqual(_first.Key);
    [Fact] void should_not_consider_the_two_sets_equal() => _first.ShouldNotEqual(_second);
}
