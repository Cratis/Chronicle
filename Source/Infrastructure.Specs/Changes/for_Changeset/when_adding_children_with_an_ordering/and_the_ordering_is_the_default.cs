// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_the_ordering_is_the_default : given.a_changeset_with_existing_children
{
    void Because()
    {
        AddChild("c", 3, null);
        AddChild("a", 1, ChildrenOrdering.Append);
    }

    [Fact] void should_append_new_children() => Ids.ShouldContainOnly(["b", "d", "c", "a"]);
    [Fact] void should_keep_the_insertion_order() => string.Join(',', Ids).ShouldEqual("b,d,c,a");
    [Fact] void should_not_carry_an_ordering_for_the_default() => _changeset.Changes.OfType<ChildAdded>().First().Ordering.ShouldBeNull();
}
