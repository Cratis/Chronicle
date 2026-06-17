// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer;

public class when_comparing_object_with_complex_collection_that_has_changed_element : given.an_object_comparer
{
    record Member(Guid Id, string Status);
    record Team(IEnumerable<Member> Members);

    Team _left;
    Team _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        var id = Guid.NewGuid();
        _left = new([new(id, "pending")]);
        _right = new([new(id, "approved")]);
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
    [Fact] void should_have_one_property_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_member_status_property_as_difference() => _differences.First().PropertyPath.Path.ShouldEqual($"{nameof(Team.Members)}.{nameof(Member.Status)}");
    [Fact] void should_hold_original_member_status_as_difference() => _differences.First().Original.ShouldEqual("pending");
    [Fact] void should_hold_changed_member_status_as_difference() => _differences.First().Changed.ShouldEqual("approved");
}
