// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_expando_objects_and_left_is_null_and_right_has_properties : given.an_object_comparer
{
    dynamic _left;
    dynamic _right;

    bool _result;

    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = null!;
        _right = new ExpandoObject();
        _right.StringValue = "FortyTwo";
        _right.IntValue = 44;
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_considered_equal() => _result.ShouldBeFalse();
    [Fact] void should_have_first_difference_be_the_string_value() => _differences.ToArray()[0].PropertyPath.Path.ShouldEqual("StringValue");
    [Fact] void should_have_second_difference_be_the_int_value() => _differences.ToArray()[1].PropertyPath.Path.ShouldEqual("IntValue");
    [Fact] void should_hold_changed_value_for_the_string_value() => _differences.ToArray()[0].Changed.ShouldEqual((string)_right.StringValue);
    [Fact] void should_hold_changed_value_for_the_int_value() => _differences.ToArray()[1].Changed.ShouldEqual((int)_right.IntValue);
}
