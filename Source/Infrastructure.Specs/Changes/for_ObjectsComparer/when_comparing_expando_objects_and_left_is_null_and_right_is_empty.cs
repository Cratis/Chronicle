// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_expando_objects_and_left_is_null_and_right_is_empty : given.an_object_comparer
{
    dynamic _left;
    dynamic _right;

    bool _result;

    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = null!;
        _right = new ExpandoObject();
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_considered_equal() => _result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => _differences.ShouldBeEmpty();
}
