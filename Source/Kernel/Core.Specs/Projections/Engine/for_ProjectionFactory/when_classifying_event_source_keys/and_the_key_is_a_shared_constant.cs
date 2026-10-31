// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_key_is_a_shared_constant : given.a_projection_with_source_keys
{
    void Establish() => SetKey("$value(shared)");

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_keep_collapsing_handling() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
