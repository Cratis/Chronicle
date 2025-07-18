// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_equal_collections : given.an_object_comparer
{
    record TheType(IEnumerable<int> Collection);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new([1, 2, 3]);
        _right = new([1, 2, 3]);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => _differences.ShouldBeEmpty();
}
