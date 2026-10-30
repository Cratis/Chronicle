// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_include_child_only_removals_with_join : given.a_projection_with_every_event_mappings_and_children
{
    void Establish()
    {
        var notes = _definition.Children["notes"];
        notes.From.Clear();
        notes.RemovedWithJoin[_noteUpdated] = new("noteId");
    }

    Task Because() => ProjectChildEvent();

    [Fact] void should_keep_child_removal_writers_conservative() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] void should_apply_parent_mappings_inside_a_join_on_the_removed_child() => _changeset.Changes.OfType<Joined>().Single().Changes.OfType<PropertiesChanged<ExpandoObject>>().Any(_ => ((IDictionary<string, object?>)_.State)["count"] is 1L).ShouldBeTrue();
    [Fact] void should_join_on_the_removed_child_identifier() => _changeset.Changes.OfType<Joined>().Single().ArrayIndexers.All.Single().Identifier.ShouldEqual(42);
    [Fact] void should_remove_the_child_from_every_parent() => _changeset.Changes.OfType<ChildRemovedFromAll>().Single().Key.ShouldEqual(42);
    [Fact] void should_not_write_parent_properties_under_the_child_key() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
}
