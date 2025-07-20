// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_checking_if_contains_known_object_by_key : Specification
{
    record Item(string Key, string Value);
    IEnumerable<Item> _items;
    bool _result;

    void Establish() => _items =
    [
        new Item("First", "First Value"),
        new Item("Second", "Second Value")
    ];

    void Because() => _result = _items.Contains(nameof(Item.Key), "Second");

    [Fact] void should_contain_the_object() => _result.ShouldBeTrue();
}
