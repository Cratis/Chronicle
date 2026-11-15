// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint;

public class when_comparing_lists : given.states
{
    ulong _original;
    ulong _sameInAnotherCollection;
    ulong _reordered;
    ulong _shorter;
    ulong _empty;
    ulong _emptyDictionary;

    void Because()
    {
        _original = Of(Expando(("items", new List<object?> { 1, "two", 3 })));
        _sameInAnotherCollection = Of(Expando(("items", new object?[] { 1, "two", 3 })));
        _reordered = Of(Expando(("items", new List<object?> { 3, "two", 1 })));
        _shorter = Of(Expando(("items", new List<object?> { 1, "two" })));
        _empty = Of(Expando(("items", new List<object?>())));
        _emptyDictionary = Of(Expando(("items", Expando())));
    }

    [Fact] void should_not_depend_on_the_collection_type() => _sameInAnotherCollection.ShouldEqual(_original);
    [Fact] void should_depend_on_the_order_of_items() => _reordered.ShouldNotEqual(_original);
    [Fact] void should_differ_when_an_item_is_missing() => _shorter.ShouldNotEqual(_original);
    [Fact] void should_tell_an_empty_list_from_an_empty_object() => _empty.ShouldNotEqual(_emptyDictionary);
}
