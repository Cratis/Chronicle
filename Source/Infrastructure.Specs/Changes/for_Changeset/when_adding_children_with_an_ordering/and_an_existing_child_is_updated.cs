// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children_with_an_ordering;

public class and_an_existing_child_is_updated : given.a_changeset_with_existing_children
{
    void Because()
    {
        var indexers = new ArrayIndexers([new ArrayIndexer(_itemsProperty, _idProperty, "b")]);
        PropertyMapper<object, ExpandoObject>[] mappers =
        [
            (_, target, arrayIndexers) =>
            {
                _rankProperty.SetValue(target, 10, arrayIndexers);
                return new PropertyDifference(_rankProperty, 2, 10, arrayIndexers);
            }
        ];
        _changeset.SetProperties(mappers, indexers);
    }

    [Fact] void should_keep_the_child_where_it_is() => string.Join(',', Ids).ShouldEqual("b,d");
}
