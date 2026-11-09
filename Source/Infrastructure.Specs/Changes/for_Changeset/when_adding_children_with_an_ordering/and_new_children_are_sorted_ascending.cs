// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_new_children_are_sorted_ascending : given.a_changeset_with_existing_children
{
    void Because()
    {
        var ordering = new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank"));
        AddChild("c", 3, ordering);
        AddChild("a", 1, ordering);
        AddChild("e", null, ordering);
    }

    [Fact] void should_order_by_the_sort_property_with_missing_values_first() => string.Join(',', Ids).ShouldEqual("e,a,b,c,d");
}
