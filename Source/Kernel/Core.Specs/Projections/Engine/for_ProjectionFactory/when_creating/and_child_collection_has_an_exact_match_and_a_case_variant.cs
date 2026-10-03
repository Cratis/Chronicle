// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_child_collection_has_an_exact_match_and_a_case_variant : given.a_child_collection_projection
{
    IProjection _projection;

    void Establish() => _readModel.Schemas[ReadModelGeneration.First].Properties["notes"] = new JsonSchemaProperty
    {
        Type = JsonObjectType.Array,
        Item = JsonSchema.FromJson("""
            { "type": "object", "properties": { "exactMatch": { "type": "boolean" } } }
            """)
    };

    async Task Because() => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []);

    [Fact] void should_prefer_the_exact_match() => _projection.ChildProjections.Single().TargetReadModelSchema.Properties.ContainsKey("exactMatch").ShouldBeTrue();
}
