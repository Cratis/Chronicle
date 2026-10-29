// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_classifying_event_source_keys;

public class and_the_identified_by_property_is_a_formatted_string : given.a_projection_with_source_keys
{
    void Establish() => _readModel.GetSchemaForLatestGeneration().Properties["Id"] = new JsonSchemaProperty { Type = JsonObjectType.String, Format = "guid" };

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, _schemas);

    [Fact] void should_not_classify_the_projection_as_event_source_keyed() => _projection.IsEventSourceKeyed.ShouldBeFalse();
}
