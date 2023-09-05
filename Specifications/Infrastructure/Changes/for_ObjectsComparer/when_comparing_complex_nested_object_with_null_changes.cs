// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_object_with_null_changes : given.an_object_comparer
{
    record ThirdLevel(string StringValue, int IntValue);
    record SecondLevel(string StringValue, int IntValue, ThirdLevel Third);
    record TopLevel(string StringValue, int IntValue, SecondLevel Second);

    TopLevel left;
    TopLevel right;

    bool result;

    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new TopLevel(null, 42, new("FortyThree", 43, new(null, 44)));
        right = new TopLevel("FortyFive", 45, new(null, 46, new("FortySeven", 47)));
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_considered_equal() => result.ShouldBeFalse();
    [Fact] void should_have_first_difference_be_the_first_property_on_top_level() => differences.ToArray()[0].PropertyPath.Path.ShouldEqual(nameof(TopLevel.StringValue).ToCamelCase());
    [Fact] void should_have_second_difference_be_the_second_property_on_top_level() => differences.ToArray()[1].PropertyPath.Path.ShouldEqual(nameof(TopLevel.IntValue).ToCamelCase());
    [Fact] void should_hold_original_value_for_first_property_on_top_level() => differences.ToArray()[0].Original.ShouldEqual(left.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_top_level() => differences.ToArray()[0].Changed.ShouldEqual(right.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_top_level() => differences.ToArray()[1].Original.ShouldEqual(left.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_top_level() => differences.ToArray()[1].Changed.ShouldEqual(right.IntValue);

    [Fact] void should_have_third_difference_be_the_first_property_on_second_level() => differences.ToArray()[2].PropertyPath.Path.ShouldEqual($"{nameof(TopLevel.Second).ToCamelCase()}.{nameof(SecondLevel.StringValue).ToCamelCase()}");
    [Fact] void should_have_forth_difference_be_the_second_property_on_second_level() => differences.ToArray()[3].PropertyPath.Path.ShouldEqual($"{nameof(TopLevel.Second).ToCamelCase()}.{nameof(SecondLevel.IntValue).ToCamelCase()}");
    [Fact] void should_hold_original_value_for_first_property_on_second_level() => differences.ToArray()[2].Original.ShouldEqual(left.Second.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_second_level() => differences.ToArray()[2].Changed.ShouldEqual(right.Second.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_second_level() => differences.ToArray()[3].Original.ShouldEqual(left.Second.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_second_level() => differences.ToArray()[3].Changed.ShouldEqual(right.Second.IntValue);

    [Fact] void should_have_fifth_difference_be_the_first_property_on_third_level() => differences.ToArray()[4].PropertyPath.Path.ShouldEqual($"{nameof(TopLevel.Second).ToCamelCase()}.{nameof(SecondLevel.Third).ToCamelCase()}.{nameof(ThirdLevel.StringValue).ToCamelCase()}");
    [Fact] void should_have_sixth_difference_be_the_second_property_on_third_level() => differences.ToArray()[5].PropertyPath.Path.ShouldEqual($"{nameof(TopLevel.Second).ToCamelCase()}.{nameof(SecondLevel.Third).ToCamelCase()}.{nameof(ThirdLevel.IntValue).ToCamelCase()}");
    [Fact] void should_hold_original_value_for_first_property_on_third_level() => differences.ToArray()[4].Original.ShouldEqual(left.Second.Third.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_third_level() => differences.ToArray()[4].Changed.ShouldEqual(right.Second.Third.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_third_level() => differences.ToArray()[5].Original.ShouldEqual(left.Second.Third.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_third_level() => differences.ToArray()[5].Changed.ShouldEqual(right.Second.Third.IntValue);
}
