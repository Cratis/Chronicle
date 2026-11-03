// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_source_keyed_projection_has_children : given.a_projection_with_source_keys
{
    void Establish() => _definition.Children["notes"] = Child();

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_preserve_the_child_projection_exclusion() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
