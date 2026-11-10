// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_a_new_child_gets_its_sort_value_later_in_the_changeset : given.a_changeset_with_existing_children
{
    void Because()
    {
        AddChild("a", null, new ChildrenOrdering(ChildrenPlacement.Sorted, new("rank")));

        var indexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, "a")]);
        PropertyMapper<object, ExpandoObject>[] mappers =
        [
            (_, target, arrayIndexers) =>
            {
                _rankProperty.SetValue(target, 3, arrayIndexers);
                return new PropertyDifference(_rankProperty, null, 3, arrayIndexers);
            }
        ];
        _changeset.SetProperties(mappers, indexers);
    }

    [Fact] void should_order_the_new_child_by_its_final_sort_value() => string.Join(',', Ids).ShouldEqual("b,a,d");
}
