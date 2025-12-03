// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_expando_object_with_changes : given.an_object_comparer
{
    record ThirdLevel(string StringValue, int IntValue);
    dynamic _left;
    dynamic _right;

    dynamic _leftChild;
    dynamic _rightChild;

    bool _result;

    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _leftChild = new ExpandoObject();
        _leftChild.StringValue = "FortySeven";
        _leftChild.IntValue = 47;

        _left = new ExpandoObject();
        _left.StringValue = "FortyTwo";
        _left.IntValue = 42;
        _left.Second = new ExpandoObject();
        _left.Second.StringValue = "FortyThree";
        _left.Second.IntValue = 43;
        _left.Second.Third = new ThirdLevel("FortyFour", 44);
#pragma warning disable IDE0300 // Simplify collection initialization
        _left.Collection = new[] { _leftChild };
#pragma warning restore IDE0300 // Simplify collection initialization

        _rightChild = new ExpandoObject();
        _rightChild.StringValue = "FortyEight";
        _rightChild.IntValue = 48;

        _right = new ExpandoObject();
        _right.StringValue = "FortyFour";
        _right.IntValue = 44;
        _right.Second = new ExpandoObject();
        _right.Second.StringValue = "FortyFive";
        _right.Second.IntValue = 45;
        _right.Second.Third = new ThirdLevel("FortySix", 46);
#pragma warning disable IDE0300 // Simplify collection initialization
        _right.Collection = new[] { _rightChild };
#pragma warning restore IDE0300 // Simplify collection initialization
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_considered_equal() => _result.ShouldBeFalse();
    [Fact] void should_have_first_difference_be_the_first_property_on_top_level() => _differences.ToArray()[0].PropertyPath.Path.ShouldEqual("StringValue");
    [Fact] void should_have_second_difference_be_the_second_property_on_top_level() => _differences.ToArray()[1].PropertyPath.Path.ShouldEqual("IntValue");
    [Fact] void should_hold_original_value_for_first_property_on_top_level() => _differences.ToArray()[0].Original.ShouldEqual((string)_left.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_top_level() => _differences.ToArray()[0].Changed.ShouldEqual((string)_right.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_top_level() => _differences.ToArray()[1].Original.ShouldEqual((int)_left.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_top_level() => _differences.ToArray()[1].Changed.ShouldEqual((int)_right.IntValue);

    [Fact] void should_have_third_difference_be_the_first_property_on_second_level() => _differences.ToArray()[2].PropertyPath.Path.ShouldEqual("Second.StringValue");
    [Fact] void should_have_forth_difference_be_the_second_property_on_second_level() => _differences.ToArray()[3].PropertyPath.Path.ShouldEqual("Second.IntValue");
    [Fact] void should_hold_original_value_for_first_property_on_second_level() => _differences.ToArray()[2].Original.ShouldEqual((string)_left.Second.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_second_level() => _differences.ToArray()[2].Changed.ShouldEqual((string)_right.Second.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_second_level() => _differences.ToArray()[3].Original.ShouldEqual((int)_left.Second.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_second_level() => _differences.ToArray()[3].Changed.ShouldEqual((int)_right.Second.IntValue);

    [Fact] void should_have_fifth_difference_be_the_first_property_on_third_level() => _differences.ToArray()[4].PropertyPath.Path.ShouldEqual("Second.Third.StringValue");
    [Fact] void should_have_sixth_difference_be_the_second_property_on_third_level() => _differences.ToArray()[5].PropertyPath.Path.ShouldEqual("Second.Third.IntValue");
    [Fact] void should_hold_original_value_for_first_property_on_third_level() => _differences.ToArray()[4].Original.ShouldEqual((string)_left.Second.Third.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_third_level() => _differences.ToArray()[4].Changed.ShouldEqual((string)_right.Second.Third.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_third_level() => _differences.ToArray()[5].Original.ShouldEqual((int)_left.Second.Third.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_third_level() => _differences.ToArray()[5].Changed.ShouldEqual((int)_right.Second.Third.IntValue);

    [Fact] void should_have_seventh_difference_be_the_first_property_on_child() => _differences.ToArray()[6].PropertyPath.Path.ShouldEqual("[Collection].StringValue");
    [Fact] void should_have_seventh_difference_be_the_second_property_on_child() => _differences.ToArray()[7].PropertyPath.Path.ShouldEqual("[Collection].IntValue");
    [Fact] void should_hold_original_value_for_first_property_on_child() => _differences.ToArray()[6].Original.ShouldEqual((string)_leftChild.StringValue);
    [Fact] void should_hold_changed_value_for_first_property_on_child() => _differences.ToArray()[6].Changed.ShouldEqual((string)_rightChild.StringValue);
    [Fact] void should_hold_original_value_for_second_property_on_child() => _differences.ToArray()[7].Original.ShouldEqual((int)_leftChild.IntValue);
    [Fact] void should_hold_changed_value_for_second_property_on_child() => _differences.ToArray()[7].Changed.ShouldEqual((int)_rightChild.IntValue);
}
