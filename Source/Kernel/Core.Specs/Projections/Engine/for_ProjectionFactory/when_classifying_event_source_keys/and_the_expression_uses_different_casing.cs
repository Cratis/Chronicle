// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_expression_uses_different_casing : given.a_projection_with_source_keys
{
    object _key;

    void Establish() => SetKey("$EventSourceId");

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);
        _key = await ResolveKey("first-source");
    }

    [Fact] void should_preserve_the_default_event_source_key_classification() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_preserve_the_default_event_source_key_resolution() => _key.ShouldEqual("first-source");
    [Fact] void should_not_change_the_stored_key_casing() => _definition.From[_first].Key.Value.ShouldEqual("$EventSourceId");
}
