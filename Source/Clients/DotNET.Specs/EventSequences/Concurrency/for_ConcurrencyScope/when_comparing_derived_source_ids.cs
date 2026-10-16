// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScope;

public class when_comparing_derived_source_ids : Specification
{
    ConcurrencyScope _base;
    ConcurrencyScope _derived;
    Dictionary<ConcurrencyScope, string> _scopes;

    void Establish()
    {
        _base = new(42, "book");
        _derived = new(42, new BookId("book"));
        _scopes = new() { [_base] = "book" };
    }

    [Fact] void should_compare_scopes_by_the_wire_source() => _derived.ShouldEqual(_base);
    [Fact] void should_have_the_same_hash_code() => _derived.GetHashCode().ShouldEqual(_base.GetHashCode());
    [Fact] void should_find_the_scope_in_a_keyed_collection() => _scopes[_derived].ShouldEqual("book");
    [Fact] void should_not_equate_different_sources() => (_derived == _base with { EventSourceId = "other" }).ShouldBeFalse();

    record BookId(string Value) : EventSourceId(Value);
}
