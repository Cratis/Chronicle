// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_finding_known_object_by_key : Specification
{
    record Item(string Key, string Value);
    IEnumerable<Item> _items;
    Item _result;

    void Establish() => _items =
    [
        new Item("First", "First Value"),
        new Item("Second", "Second Value")
    ];

    void Because() => _result = _items.FindByKey(nameof(Item.Key), "Second");

    [Fact] void should_return_correct_item() => _result.ShouldEqual(_items.ToArray()[1]);
}
