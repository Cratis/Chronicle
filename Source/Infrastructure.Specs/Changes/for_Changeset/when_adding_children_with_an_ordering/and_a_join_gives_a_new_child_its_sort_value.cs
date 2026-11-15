// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_a_join_gives_a_new_child_its_sort_value : given.a_changeset_with_existing_children
{
    void Because()
    {
        AddChild("a", null, new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank")));

        var indexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, "a")]);
        var joined = _changeset.ResolvedJoin(_idProperty, "a", new object(), indexers);
        joined.SetProperties([SetRank], indexers);
    }

    [Fact] void should_order_the_new_child_by_the_joined_sort_value() => string.Join(',', Ids).ShouldEqual("b,a,d");

    static PropertyDifference SetRank(object source, ExpandoObject target, ArrayIndexers indexers)
    {
        _rankProperty.SetValue(target, 3, indexers);
        return new PropertyDifference(_rankProperty, null, 3, indexers);
    }
}
