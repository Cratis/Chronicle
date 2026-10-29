// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_expression_contains_surrounding_spaces : given.a_projection_with_source_keys
{
    void Establish() => SetKey(" $eventSourceId ");

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_not_treat_the_content_path_as_the_exact_expression() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] void should_not_trim_the_stored_key() => _definition.From[_first].Key.Value.ShouldEqual(" $eventSourceId ");
}
