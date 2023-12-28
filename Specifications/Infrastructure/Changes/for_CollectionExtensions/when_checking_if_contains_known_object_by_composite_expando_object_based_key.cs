// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Changes.for_CollectionExtensions;

public class when_checking_if_contains_known_object_by_composite_expando_object_based_key : Specification
{
    record Item(ExpandoObject Key, string Value);
    IEnumerable<Item> items;
    bool result;

    ExpandoObject first_key;
    ExpandoObject second_key;

    void Establish()
    {
        first_key = new ExpandoObject();

        ((dynamic)first_key).FirstCompositeProperty = "FirstKey FirstProperty";
        ((dynamic)first_key).SecondCompositeProperty = "FirstKey SecondProperty";

        second_key = new ExpandoObject();
        ((dynamic)second_key).FirstCompositeProperty = "SecondKey FirstProperty";
        ((dynamic)second_key).SecondCompositeProperty = "SecondKey SecondProperty";

        items = new[]
        {
            new Item(first_key, "First Value"),
            new Item(second_key, "Second Value")
        };
    }

    void Because() => result = items.Contains(nameof(Item.Key), second_key);

    [Fact] void should_contain_the_object() => result.ShouldBeTrue();
}
