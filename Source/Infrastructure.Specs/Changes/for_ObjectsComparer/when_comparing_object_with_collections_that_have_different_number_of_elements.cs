// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Strings;

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_collections_that_have_different_number_of_elements : given.an_object_comparer
{
    record TheType(IEnumerable<int> Collection);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new([1, 2, 3]);
        _right = new([4, 5]);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_collection_property_as_difference() => _differences.First().PropertyPath.Path.ShouldEqual(nameof(TheType.Collection).ToCamelCase());
}
