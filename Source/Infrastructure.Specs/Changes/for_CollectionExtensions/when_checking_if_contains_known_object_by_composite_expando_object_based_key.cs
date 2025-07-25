// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_CollectionExtensions;

public class when_checking_if_contains_known_object_by_composite_expando_object_based_key : Specification
{
    record Item(ExpandoObject Key, string Value);
    IEnumerable<Item> _items;
    bool _result;

    ExpandoObject _firstKey;
    ExpandoObject _secondKey;

    void Establish()
    {
        _firstKey = new ExpandoObject();

        ((dynamic)_firstKey).FirstCompositeProperty = "FirstKey FirstProperty";
        ((dynamic)_firstKey).SecondCompositeProperty = "FirstKey SecondProperty";

        _secondKey = new ExpandoObject();
        ((dynamic)_secondKey).FirstCompositeProperty = "SecondKey FirstProperty";
        ((dynamic)_secondKey).SecondCompositeProperty = "SecondKey SecondProperty";

        _items =
        [
            new Item(_firstKey, "First Value"),
            new Item(_secondKey, "Second Value")
        ];
    }

    void Because() => _result = _items.Contains(nameof(Item.Key), _secondKey);

    [Fact] void should_contain_the_object() => _result.ShouldBeTrue();
}
