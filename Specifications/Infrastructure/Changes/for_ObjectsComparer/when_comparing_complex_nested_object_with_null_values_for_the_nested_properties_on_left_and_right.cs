// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_object_with_null_values_for_the_nested_properties_on_left_and_right : given.an_object_comparer
{
    record TopLevel(SecondLevel? Second);
    record SecondLevel(string StringValue, int IntValue);

    TopLevel left;
    TopLevel right;
    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(null);
        right = new(null);
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_be_considered_equal() => result.ShouldBeTrue();
    [Fact] void should_not_have_any_differences() => differences.ShouldBeEmpty();
}
