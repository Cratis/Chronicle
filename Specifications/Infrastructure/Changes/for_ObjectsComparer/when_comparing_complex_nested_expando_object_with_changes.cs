// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Aksio.Cratis.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_expando_object_with_changes : given.an_object_comparer
{
    record ThirdLevel(string StringValue, int IntValue);
    dynamic left;
    dynamic right;

    dynamic left_child;
    dynamic right_child;

    bool result;

    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left_child = new ExpandoObject();
        left_child.StringValue = "FortySeven";
        left_child.IntValue = 47;

        left = new ExpandoObject();
        left.StringValue = "FortyTwo";
        left.IntValue = 42;
        left.Second = new ExpandoObject();
        left.Second.StringValue = "FortyThree";
        left.Second.IntValue = 43;
        left.Second.Third = new ThirdLevel("FortyFour", 44);
        left.Collection = new[] { left_child };

        right_child = new ExpandoObject();
        right_child.StringValue = "FortyEight";
        right_child.IntValue = 48;

        right = new ExpandoObject();
        right.StringValue = "FortyFour";
        right.IntValue = 44;
        right.Second = new ExpandoObject();
        right.Second.StringValue = "FortyFive";
        right.Second.IntValue = 45;
        right.Second.Third = new ThirdLevel("FortySix", 46);
        right.Collection = new[] { right_child };
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_considered_equal() => result.ShouldBeFalse();
    [Fact] void should_have_first_difference_be_the_first_property_on_top_level() => differences.ToArray()[0].PropertyPath.Path.ShouldEqual("stringValue");
    [Fact] void should_have_second_difference_be_the_second_property_on_top_level() => differences.ToArray()[1].PropertyPath.Path.ShouldEqual("intValue");
    [Fact] void should_hold_original_value_for_first_property_on_top_level() => differences.ToArray()[0].Original.ShouldEqual((string)left.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_top_level() => differences.ToArray()[0].Changed.ShouldEqual((string)right.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_top_level() => differences.ToArray()[1].Original.ShouldEqual((int)left.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_top_level() => differences.ToArray()[1].Changed.ShouldEqual((int)right.IntValue);

    [Fact] void should_have_third_difference_be_the_first_property_on_second_level() => differences.ToArray()[2].PropertyPath.Path.ShouldEqual("second.stringValue");
    [Fact] void should_have_forth_difference_be_the_second_property_on_second_level() => differences.ToArray()[3].PropertyPath.Path.ShouldEqual("second.intValue");
    [Fact] void should_hold_original_value_for_first_property_on_second_level() => differences.ToArray()[2].Original.ShouldEqual((string)left.Second.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_second_level() => differences.ToArray()[2].Changed.ShouldEqual((string)right.Second.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_second_level() => differences.ToArray()[3].Original.ShouldEqual((int)left.Second.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_second_level() => differences.ToArray()[3].Changed.ShouldEqual((int)right.Second.IntValue);

    [Fact] void should_have_fifth_difference_be_the_first_property_on_third_level() => differences.ToArray()[4].PropertyPath.Path.ShouldEqual("second.third.stringValue");
    [Fact] void should_have_sixth_difference_be_the_second_property_on_third_level() => differences.ToArray()[5].PropertyPath.Path.ShouldEqual("second.third.intValue");
    [Fact] void should_hold_original_value_for_first_property_on_third_level() => differences.ToArray()[4].Original.ShouldEqual((string)left.Second.Third.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_third_level() => differences.ToArray()[4].Changed.ShouldEqual((string)right.Second.Third.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_third_level() => differences.ToArray()[5].Original.ShouldEqual((int)left.Second.Third.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_third_level() => differences.ToArray()[5].Changed.ShouldEqual((int)right.Second.Third.IntValue);

    [Fact] void should_have_seventh_difference_be_the_first_property_on_child() => differences.ToArray()[6].PropertyPath.Path.ShouldEqual("[collection].stringValue");
    [Fact] void should_have_seventh_difference_be_the_second_property_on_child() => differences.ToArray()[7].PropertyPath.Path.ShouldEqual("[collection].intValue");
    [Fact] void should_hold_original_value_for_first_property_on_child() => differences.ToArray()[6].Original.ShouldEqual((string)left_child.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_child() => differences.ToArray()[6].Changed.ShouldEqual((string)right_child.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_child() => differences.ToArray()[7].Original.ShouldEqual((int)left_child.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_child() => differences.ToArray()[7].Changed.ShouldEqual((int)right_child.IntValue);
}
