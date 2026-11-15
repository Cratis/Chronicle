// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation.Reducers.for_ReadModelStateFingerprint;

public class when_comparing_dictionaries : given.states
{
    ulong _original;
    ulong _reordered;
    ulong _differentValue;
    ulong _differentKey;
    ulong _extraEntry;
    ulong _nestedDifferent;
    ulong _nestedSame;

    void Because()
    {
        _original = Of(Expando(("name", "a"), ("count", 1), ("nested", Expando(("x", 1), ("y", 2)))));
        _reordered = Of(Expando(("nested", Expando(("y", 2), ("x", 1))), ("count", 1), ("name", "a")));
        _differentValue = Of(Expando(("name", "b"), ("count", 1), ("nested", Expando(("x", 1), ("y", 2)))));
        _differentKey = Of(Expando(("title", "a"), ("count", 1), ("nested", Expando(("x", 1), ("y", 2)))));
        _extraEntry = Of(Expando(("name", "a"), ("count", 1), ("nested", Expando(("x", 1), ("y", 2))), ("more", null)));
        _nestedDifferent = Of(Expando(("name", "a"), ("count", 1), ("nested", Expando(("x", 1), ("y", 3)))));
        _nestedSame = Of(Expando(("name", "a"), ("count", 1), ("nested", Expando(("x", 1), ("y", 2)))));
    }

    [Fact] void should_not_depend_on_the_order_of_entries() => _reordered.ShouldEqual(_original);
    [Fact] void should_be_deterministic() => _nestedSame.ShouldEqual(_original);
    [Fact] void should_differ_when_a_value_differs() => _differentValue.ShouldNotEqual(_original);
    [Fact] void should_differ_when_a_key_differs() => _differentKey.ShouldNotEqual(_original);
    [Fact] void should_differ_when_an_entry_is_added() => _extraEntry.ShouldNotEqual(_original);
    [Fact] void should_differ_when_a_nested_value_differs() => _nestedDifferent.ShouldNotEqual(_original);
}
