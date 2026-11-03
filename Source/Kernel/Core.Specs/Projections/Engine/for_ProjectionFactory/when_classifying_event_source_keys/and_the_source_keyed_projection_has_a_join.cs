// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_source_keyed_projection_has_a_join : given.a_projection_with_source_keys
{
    void Establish() => _definition.Join[_second] = new("Id", new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId);

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_preserve_conservative_join_handling() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
