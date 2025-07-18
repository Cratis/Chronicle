// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_object_with_null_values_for_the_nested_properties_on_left_and_right : given.an_object_comparer
{
    record TopLevel(SecondLevel? Second);
    record SecondLevel(string StringValue, int IntValue);

    TopLevel _left;
    TopLevel _right;
    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new(null);
        _right = new(null);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_considered_equal() => _result.ShouldBeTrue();
    [Fact] void should_not_have_any_differences() => _differences.ShouldBeEmpty();
}
