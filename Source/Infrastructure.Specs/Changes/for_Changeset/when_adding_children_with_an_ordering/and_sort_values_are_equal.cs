// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_sort_values_are_equal : given.a_changeset_with_existing_children
{
    void Because()
    {
        var ordering = new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank"), SortDirection.Descending);
        AddChild("c", 2, ordering);
        AddChild("a", 2, ordering);
    }

    [Fact] void should_order_equal_sort_values_by_the_identifier_ascending() => string.Join(',', Ids).ShouldEqual("d,a,b,c");
}
