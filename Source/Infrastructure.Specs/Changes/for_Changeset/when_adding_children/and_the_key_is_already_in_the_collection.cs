// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children;

public class and_the_key_is_already_in_the_collection : given.a_changeset_with_an_items_collection
{
    int _changesBefore;

    void Establish()
    {
        var existing = new ExpandoObject();
        ((IDictionary<string, object?>)existing)["id"] = "a";
        ((IDictionary<string, object?>)_changeset.InitialState)["items"] = new List<object> { existing };
    }

    void Because()
    {
        _changesBefore = _changeset.Changes.Count();
        _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, "a", [SetName("one")], IndexersFor("b"));
    }

    [Fact] void should_not_record_a_change() => _changeset.Changes.Count().ShouldEqual(_changesBefore);
    [Fact] void should_not_add_a_child() => Items.Count().ShouldEqual(1);
}
