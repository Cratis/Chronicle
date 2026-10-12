// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_exclude_child_only_events : given.a_projection_with_every_event_mappings_and_children
{
    void Establish() => _definition = _definition with { FromEvery = _definition.FromEvery with { IncludeChildren = false } };

    Task Because() => ProjectChildEvent();

    [Fact] void should_leave_the_parent_timestamp_unchanged() => _result["lastUpdated"].ShouldEqual(DateTimeOffset.MinValue);
    [Fact] void should_not_count_the_child_event_at_the_root() => _result["count"].ShouldEqual(0L);
}
