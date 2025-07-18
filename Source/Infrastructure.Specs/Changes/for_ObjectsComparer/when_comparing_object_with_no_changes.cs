// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_no_changes : given.an_object_comparer
{
    record TheType(string StringValue, int IntValue);

    TheType _left;
    TheType _right;

    bool _result;

    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new TheType("FortyTwo", 42);
        _right = new TheType("FortyTwo", 42);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_considered_equal() => _result.ShouldBeTrue();
    [Fact] void should_not_have_any_differences() => _differences.ShouldBeEmpty();
}
