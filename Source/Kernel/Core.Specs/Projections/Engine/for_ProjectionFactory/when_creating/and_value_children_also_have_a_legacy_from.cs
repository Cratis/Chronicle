// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_value_children_also_have_a_legacy_from : given.a_value_child_projection
{
    Changeset<AppendedEvent, ExpandoObject> _first;
    Changeset<AppendedEvent, ExpandoObject> _repeated;
    Changeset<AppendedEvent, ExpandoObject> _deleted;
    Changeset<AppendedEvent, ExpandoObject> _recreated;
    Changeset<AppendedEvent, ExpandoObject> _afterRecreation;

    protected override bool IncludeLegacyFrom => true;

    async Task Because()
    {
        await Project(_created);
        _first = await Project(_valueAdded, "first");
        await Project(_valueAdded, "second");
        _repeated = await Project(_valueAdded, "first");
        _deleted = await Project(_removed);
        _recreated = await Project(_created);
        _afterRecreation = await Project(_valueAdded, "third");
    }

    [Fact] void should_append_the_first_value_once() => Values(_first).ShouldEqual(["first"]);
    [Fact] void should_record_only_one_addition_per_event() => _first.Changes.OfType<ChildAdded>().Count().ShouldEqual(1);
    [Fact] void should_not_assign_an_object_key() => _first.Changes.OfType<ChildAdded>().Single().Key.ShouldBeNull();
    [Fact] void should_preserve_order_and_repeated_values() => Values(_repeated).ShouldEqual(["first", "second", "first"]);
    [Fact] void should_remove_the_read_model() => _deleted.Changes.OfType<Removed>().Count().ShouldEqual(1);
    [Fact] void should_recreate_with_an_empty_collection() => Values(_recreated).ShouldBeEmpty();
    [Fact] void should_append_after_recreation() => Values(_afterRecreation).ShouldEqual(["third"]);
}
