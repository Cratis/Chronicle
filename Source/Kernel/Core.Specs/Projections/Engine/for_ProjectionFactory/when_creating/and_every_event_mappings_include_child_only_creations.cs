// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_include_child_only_creations : given.a_projection_with_every_event_mappings_and_children
{
    void Establish() => _noteExists = false;

    Task Because() => ProjectChildEvent();

    [Fact] void should_update_the_parent_root() => _result["lastUpdated"].ShouldEqual(_occurred.UtcDateTime);
    [Fact] void should_count_the_child_event_once() => _result["count"].ShouldEqual(1L);
    [Fact] void should_add_the_child() => _changeset.Changes.OfType<ChildAdded>().Single().Key.ShouldEqual(42);
    [Fact] void should_record_the_parent_update_as_a_parent_change() => _changeset.Changes.OfType<PropertiesChanged<ExpandoObject>>().SelectMany(_ => _.Differences).Single(_ => _.PropertyPath.Path == "lastUpdated").ArrayIndexers.IsEmpty.ShouldBeTrue();
    [Fact] void should_not_put_parent_properties_on_the_added_child() => ((IDictionary<string, object?>)_changeset.Changes.OfType<ChildAdded>().Single().Child).ContainsKey("lastUpdated").ShouldBeFalse();
}
