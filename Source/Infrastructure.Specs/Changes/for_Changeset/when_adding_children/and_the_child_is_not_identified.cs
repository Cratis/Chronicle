// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes.for_Changeset.when_adding_children;

public class and_the_child_is_not_identified : given.a_changeset_with_an_items_collection
{
    void Because()
    {
        _changeset.AddChild<System.Dynamic.ExpandoObject>(_itemsProperty, PropertyPath.NotSet, "first", [SetName("one")], IndexersFor(0, PropertyPath.NotSet));
        _changeset.AddChild<System.Dynamic.ExpandoObject>(_itemsProperty, PropertyPath.NotSet, "second", [SetName("two")], IndexersFor(0, PropertyPath.NotSet));
    }

    [Fact] void should_append_both_children() => string.Join(',', Items.Select(_ => _["name"])).ShouldEqual("one,two");
    [Fact] void should_record_both_children() => _changeset.Changes.OfType<ChildAdded>().Count().ShouldEqual(2);
    [Fact] void should_address_the_first_child_by_its_appended_index() => _changeset.Changes.OfType<ChildAdded>().First().ArrayIndexers.All.Single().Identifier.ShouldEqual(0);
    [Fact] void should_address_the_second_child_by_its_appended_index() => _changeset.Changes.OfType<ChildAdded>().Last().ArrayIndexers.All.Single().Identifier.ShouldEqual(1);
}
