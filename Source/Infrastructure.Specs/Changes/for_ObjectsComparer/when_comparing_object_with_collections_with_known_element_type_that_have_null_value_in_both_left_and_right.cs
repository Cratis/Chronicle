// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_collections_with_known_element_type_that_have_null_value_in_both_left_and_right : given.an_object_comparer
{
    record TheType(IEnumerable<string> Collection);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new([null!]);
        right = new([null!]);
    }

    void Because() => result = comparer.Compare(left, right, out differences);

    [Fact] void should_be_equal() => result.ShouldBeTrue();
    [Fact] void should_have_no_property_difference() => differences.Count().ShouldEqual(0);
}
