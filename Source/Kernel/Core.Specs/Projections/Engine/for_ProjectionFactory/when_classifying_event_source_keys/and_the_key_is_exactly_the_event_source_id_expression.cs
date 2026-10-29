// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_key_is_exactly_the_event_source_id_expression : given.a_projection_with_source_keys
{
    object _firstKey;
    object _secondKey;

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);
        _firstKey = await ResolveKey("first-source");
        _secondKey = await ResolveKey("second-source");
    }

    [Fact] void should_classify_the_projection_as_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_resolve_the_first_metadata_id() => _firstKey.ShouldEqual("first-source");
    [Fact] void should_resolve_the_second_metadata_id() => _secondKey.ShouldEqual("second-source");
    [Fact] void should_preserve_the_key_expression() => _definition.From[_first].Key.Value.ShouldEqual(WellKnownExpressions.EventSourceId);
}
