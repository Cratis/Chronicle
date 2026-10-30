// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_key_names_a_payload_event_source_id : given.a_projection_with_source_keys
{
    object _key;

    void Establish() => SetKey("EventSourceId");

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);
        _key = await ResolveKey("metadata-source");
    }

    [Fact] void should_keep_collapsing_handling() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] void should_preserve_payload_key_resolution() => _key.ShouldEqual("payload-source");
}
