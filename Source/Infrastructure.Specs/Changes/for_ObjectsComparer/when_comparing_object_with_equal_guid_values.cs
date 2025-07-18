// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_equal_guid_values : given.an_object_comparer
{
    record TheType(Guid TheGuid);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new(Guid.Parse("240409fd-43db-4675-9345-025526c9c7e4"));
        _right = new(Guid.Parse("240409fd-43db-4675-9345-025526c9c7e4"));
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_be_equal() => _result.ShouldBeTrue();
    [Fact] void should_have_no_differences() => _differences.ShouldBeEmpty();
}
