// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_finding_an_object_by_a_value_equal_composite_key : Specification
{
    record Item(ExpandoObject Key, string Value);

    Item[] _items;
    ExpandoObject _key;
    Item _result;

    void Establish()
    {
        _items =
        [
            new(new { tenantId = "other", itemId = "item" }.AsExpandoObject(), "other tenant"),
            new(new { tenantId = "tenant", itemId = "other" }.AsExpandoObject(), "other item"),
            new(new { tenantId = "tenant", itemId = "item" }.AsExpandoObject(), "target")
        ];
        _key = new { tenantId = "tenant", itemId = "item" }.AsExpandoObject();
    }

    void Because() => _result = _items.FindByKey(nameof(Item.Key), _key);

    [Fact] void should_find_the_child_with_both_key_parts() => _result.ShouldEqual(_items[2]);
    [Fact] void should_not_depend_on_key_reference_equality() => ReferenceEquals(_items[2].Key, _key).ShouldBeFalse();
}
