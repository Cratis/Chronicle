// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Aksio.Strings;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_object_with_collections_with_different_element_type_on_left_that_have_null_value_in_left_but_values_in_right : given.an_object_comparer
{
    record TheType(IEnumerable Collection);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(new object[] { null! });
        right = new(new string[] { "1" });
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_equal() => result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => differences.Count().ShouldEqual(1);
    [Fact] void should_have_collection_property_as_difference() => differences.First().PropertyPath.Path.ShouldEqual(nameof(TheType.Collection).ToCamelCase());
}
