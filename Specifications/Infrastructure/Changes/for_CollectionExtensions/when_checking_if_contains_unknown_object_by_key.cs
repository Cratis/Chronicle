// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_checking_if_contains_unknown_object_by_key : Specification
{
    record Item(string Key, string Value);
    IEnumerable<Item> items;
    bool result;

    void Establish() => items =
    [
        new Item("First", "First Value"),
        new Item("Second", "Second Value")
    ];

    void Because() => result = items.Contains(nameof(Item.Key), "Third");

    [Fact] void should_not_contain_the_object() => result.ShouldBeFalse();
}
