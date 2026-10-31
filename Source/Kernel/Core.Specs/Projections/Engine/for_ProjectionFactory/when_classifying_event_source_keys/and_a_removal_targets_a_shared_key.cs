// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_a_removal_targets_a_shared_key : given.a_projection_with_source_keys
{
    void Establish() => _definition.RemovedWith[_second] = new("$value(shared)", null);

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_not_bypass_collapsing_safety_for_removals() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
