// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_identified_by_property_is_absent : given.a_projection_with_source_keys
{
    object _key;

    void Establish() => _readModel.GetSchemaForLatestGeneration().Properties.Remove("Id");

    async Task Because()
    {
        _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);
        _key = await ResolveKey("01");
    }

    [Fact] void should_classify_the_projection_as_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeTrue();
    [Fact] void should_preserve_the_metadata_id() => _key.ShouldEqual("01");
}
