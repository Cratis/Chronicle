// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_include_nested_child_only_events : given.a_projection_with_every_event_mappings_and_children
{
    void Establish()
    {
        var notes = _definition.Children["notes"];
        _definition.Children["notes"] = notes with { FromEvery = new(new Dictionary<PropertyPath, string> { ["count"] = "$count" }, true) };
        var replies = Child();
        replies.From[_replyUpdated] = new(new Dictionary<PropertyPath, string>(), "replyId", "noteId");
        notes.Children["replies"] = replies;
    }

    Task Because() => ProjectChildEvent(nested: true);

    [Fact] void should_update_the_root_from_the_grandchild_event() => _result["lastUpdated"].ShouldEqual(_occurred.UtcDateTime);
    [Fact] void should_count_the_grandchild_event_once_at_the_root() => _result["count"].ShouldEqual(1L);
    [Fact] void should_resolve_the_containing_root_through_the_parent_event() => _key.Value.ShouldEqual("parent");
    [Fact] void should_keep_both_child_identifiers_in_the_hierarchy() => _key.ArrayIndexers.All.Count().ShouldEqual(2);
    [Fact] void should_update_the_containing_child() => ((IDictionary<string, object?>)((IEnumerable<object>)_result["notes"]!).Single())["count"].ShouldEqual(1L);
}
