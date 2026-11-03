// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_a_composite_key_contains_the_event_source_id : given.a_projection_with_source_keys
{
    void Establish() => SetKey("$composite(id=$eventSourceId)");

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_keep_composite_keys_conservative() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
