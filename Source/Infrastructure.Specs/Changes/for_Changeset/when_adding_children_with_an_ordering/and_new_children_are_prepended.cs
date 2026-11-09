// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_new_children_are_prepended : given.a_changeset_with_existing_children
{
    static readonly ChildrenOrdering _ordering = new(ChildrenPlacement.Prepend, PropertyPath.NotSet);

    void Because()
    {
        AddChild("c", 3, _ordering);
        AddChild("a", 1, _ordering);
    }

    [Fact] void should_insert_each_new_child_first() => string.Join(',', Ids).ShouldEqual("a,c,b,d");
    [Fact] void should_carry_the_ordering_to_the_sink() => _changeset.Changes.OfType<ChildAdded>().All(_ => _.Ordering == _ordering).ShouldBeTrue();
}
