// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_complex_nested_object_with_null_values_for_the_nested_properties_on_right : given.an_object_comparer
{
    record SecondLevel(string StringValue, int IntValue);
    record TopLevel(SecondLevel? Second);

    TopLevel _left;
    TopLevel _right;
    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new(new(string.Empty, 0));
        _right = new(null);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_considered_equal() => _result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_nested_property_as_difference() => _differences.First().PropertyPath.Path.ShouldEqual(nameof(TopLevel.Second));
}
