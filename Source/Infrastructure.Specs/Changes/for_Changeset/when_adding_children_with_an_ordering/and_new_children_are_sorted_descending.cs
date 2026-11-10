// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_new_children_are_sorted_descending : given.a_changeset_with_existing_children
{
    void Because()
    {
        var ordering = new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank"), SortDirection.Descending);
        AddChild("c", 3, ordering);
        AddChild("z", null, ordering);
        AddChild("e", 5, ordering);
    }

    [Fact] void should_order_by_the_sort_property_with_missing_values_last() => string.Join(',', Ids).ShouldEqual("e,d,c,b,z");
}
