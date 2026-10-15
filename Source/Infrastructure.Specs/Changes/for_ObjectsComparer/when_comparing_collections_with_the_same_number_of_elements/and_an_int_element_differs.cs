// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer.when_comparing_collections_with_the_same_number_of_elements;

public class and_an_int_element_differs : given.an_object_comparer
{
    record TheType(int[] Collection);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new([1, 2, 3]);
        _right = new([1, 42, 3]);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
    [Fact] void should_have_one_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_the_collection_as_the_difference() => _differences.Single().PropertyPath.Path.ShouldEqual(nameof(TheType.Collection));
    [Fact] void should_have_the_full_original_collection() => _differences.Single().Original.ShouldEqual(_left.Collection);
    [Fact] void should_have_the_full_changed_collection() => _differences.Single().Changed.ShouldEqual(_right.Collection);
}
