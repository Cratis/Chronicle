// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_every_event_mappings_handle_shared_root_and_child_events_once : given.a_projection_with_every_event_mappings_and_children
{
    void Establish() => _definition.From[_noteUpdated] = new(new Dictionary<PropertyPath, string>(), "parentId", null);

    Task Because() => ProjectChildEvent();

    [Fact] void should_apply_the_root_mapping_only_once() => _result["count"].ShouldEqual(1L);
}
