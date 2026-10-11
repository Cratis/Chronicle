// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_include_child_only_joins : given.a_projection_with_every_event_mappings_and_children
{
    void Establish()
    {
        var notes = _definition.Children["notes"];
        notes.From.Clear();
        notes.Join[_noteUpdated] = new("id", new Dictionary<PropertyPath, string>(), "noteId");
    }

    Task Because() => ProjectChildEvent();

    [Fact] void should_apply_parent_mappings_inside_the_join() => _changeset.Changes.OfType<Joined>().SelectMany(_ => _.Changes).OfType<PropertiesChanged<ExpandoObject>>().Any(_ => ((IDictionary<string, object?>)_.State)["count"] is 1L).ShouldBeTrue();
    [Fact] void should_not_write_parent_properties_under_the_join_source_key() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().ShouldBeEmpty();
}
