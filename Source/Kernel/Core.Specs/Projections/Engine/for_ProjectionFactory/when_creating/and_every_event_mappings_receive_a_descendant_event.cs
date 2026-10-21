// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_receive_a_descendant_event : given.a_projection_with_every_event_mappings_and_children
{
    async Task Because()
    {
        var context = await CreateChildEventContext();
        _projection.OnDescendantEvent(context);
        _result = _changeset.CurrentState;
    }

    [Fact] void should_update_the_parent_root() => _result["lastUpdated"].ShouldEqual(_occurred.UtcDateTime);
    [Fact] void should_count_the_child_event_once() => _result["count"].ShouldEqual(1L);
    [Fact] void should_not_project_the_event_into_the_child() => ((IDictionary<string, object?>)((IEnumerable<object>)_result["notes"]!).Single())["count"].ShouldEqual(0L);
}
