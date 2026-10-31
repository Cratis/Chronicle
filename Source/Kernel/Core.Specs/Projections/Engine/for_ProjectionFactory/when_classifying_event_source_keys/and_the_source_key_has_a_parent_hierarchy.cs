// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_source_key_has_a_parent_hierarchy : given.a_projection_with_source_keys
{
    void Establish()
    {
        // ParentKey participates in hierarchy resolution only on a structural child projection.
        var notes = Child();
        notes.From[_second] = new(new Dictionary<PropertyPath, string>(), WellKnownExpressions.EventSourceId, WellKnownExpressions.EventSourceId);
        _definition.Children["notes"] = notes;
    }

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_preserve_collapsing_parent_document_handling() => _projection.IsEventSourceKeyed.ShouldBeFalse();
    [Fact] void should_preserve_collapsing_child_document_handling() => _projection.ChildProjections.Single().IsEventSourceKeyed.ShouldBeFalse();
}
