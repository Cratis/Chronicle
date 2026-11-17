// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children;

public class and_a_child_added_in_this_changeset_matches_the_parent_key : given.a_changeset_with_an_items_collection
{
    void Establish() => _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, "p", [], IndexersFor("p"));

    void Because() => _changeset.AddChild<ExpandoObject>(_itemsProperty, _idProperty, "p", [SetName("updated")], IndexersFor("p"));

    [Fact] void should_keep_one_change() => _changeset.Changes.Count().ShouldEqual(1);
    [Fact] void should_keep_one_child() => Items.Count().ShouldEqual(1);
    [Fact] void should_update_the_child_in_the_state() => Items.Single()["name"].ShouldEqual("updated");
    [Fact] void should_update_the_child_in_the_recorded_change() => ((IDictionary<string, object?>)_changeset.Changes.OfType<ChildAdded>().Single().Child)["name"].ShouldEqual("updated");
    [Fact] void should_keep_the_key_of_the_recorded_change() => _changeset.Changes.OfType<ChildAdded>().Single().Key.ShouldEqual("p");
}
