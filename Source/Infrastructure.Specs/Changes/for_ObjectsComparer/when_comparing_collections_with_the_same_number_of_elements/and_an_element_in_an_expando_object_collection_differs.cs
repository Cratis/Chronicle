// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_ObjectComparer.when_comparing_collections_with_the_same_number_of_elements;

public class and_an_element_in_an_expando_object_collection_differs : given.an_object_comparer
{
    const string InvolvedUsers = "involvedUsers";

    ExpandoObject _left;
    ExpandoObject _right;
    List<object> _leftUsers;
    List<object> _rightUsers;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _leftUsers = ["a", "cratis"];
        _rightUsers = ["woksin", "cratis"];
        _left = new();
        _right = new();
        ((IDictionary<string, object?>)_left)[InvolvedUsers] = _leftUsers;
        ((IDictionary<string, object?>)_right)[InvolvedUsers] = _rightUsers;
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
    [Fact] void should_have_one_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_the_collection_as_the_difference() => _differences.Single().PropertyPath.LastSegment.Value.ShouldEqual(InvolvedUsers);
    [Fact] void should_have_the_full_original_collection() => _differences.Single().Original.ShouldEqual(_leftUsers);
    [Fact] void should_have_the_full_changed_collection() => _differences.Single().Changed.ShouldEqual(_rightUsers);
}
