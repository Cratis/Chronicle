// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_all_event_mappings_include_child_only_events : given.a_projection_with_every_event_mappings_and_children
{
    void Establish() => _definition = _definition with { SubscribesToAllEvents = true };

    Task Because() => ProjectChildEvent();

    [Fact] void should_count_the_child_event_once() => _result["count"].ShouldEqual(1L);
    [Fact] void should_update_the_parent_root() => _result["lastUpdated"].ShouldEqual(_occurred.UtcDateTime);
    [Fact] void should_use_the_childs_parent_key() => _key.Value.ShouldEqual("parent");
}
