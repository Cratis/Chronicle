// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_children_come_only_from_an_event_property : given.a_value_child_projection
{
    Changeset<AppendedEvent, ExpandoObject> _result;
    Changeset<AppendedEvent, ExpandoObject> _otherRoot;

    async Task Because()
    {
        await Project(_created);
        await Project(_valueAdded, "first");
        await Project(_created, eventSourceId: "other-parent");
        _otherRoot = await Project(_valueAdded, "other", "other-parent");
        _result = await Project(_valueAdded, "second");
    }

    [Fact] void should_append_the_values() => Values(_result).ShouldEqual(["first", "second"]);
    [Fact] void should_keep_roots_independent() => Values(_otherRoot).ShouldEqual(["other"]);
    [Fact] void should_record_only_one_child_addition() => _result.Changes.OfType<ChildAdded>().Count().ShouldEqual(1);
    [Fact] void should_leave_the_key_unset() => _result.Changes.OfType<ChildAdded>().Single().Key.ShouldBeNull();
}
