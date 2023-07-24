// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_object_with_null_values_for_the_nested_properties_on_left : given.an_object_comparer
{
    record SecondLevel(string StringValue, int IntValue);
    record TopLevel(SecondLevel? Second);

    TopLevel left;
    TopLevel right;
    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(null);
        right = new(new(string.Empty, 0));
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_considered_equal() => result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => differences.Count().ShouldEqual(1);
    [Fact] void should_have_nested_property_as_difference() => differences.First().PropertyPath.Path.ShouldEqual(nameof(TopLevel.Second).ToCamelCase());
}
