// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_an_open_schema_child_has_an_explicit_join_identifier : given.a_child_collection_projection
{
    Exception _error;
    IProjection _projection;

    void Establish()
    {
        var added = new EventType("ItemAdded", EventTypeGeneration.First);
        var renamed = new EventType("ItemRenamed", EventTypeGeneration.First);
        _readModel = _readModel with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema>
            {
                [ReadModelGeneration.First] = JsonSchema.FromJson("""
                    { "type": "object", "properties": {
                      "id": { "type": "string" },
                      "items": { "type": "array", "items": { "type": "object" } }
                    } }
                    """)
            }
        };
        _definition = _definition with
        {
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["items"] = Child() with
                {
                    IdentifiedBy = "itemId",
                    From = new Dictionary<EventType, FromDefinition>
                    {
                        [added] = new(
                            new Dictionary<PropertyPath, string>
                            {
                                ["itemId"] = "itemId", ["name"] = "name", ["_derivedTypeId"] = "$value(line)"
                            },
                            "itemId",
                            "orderId")
                    },
                    Join = new Dictionary<EventType, JoinDefinition>
                    {
                        [renamed] = new("itemId", new Dictionary<PropertyPath, string> { ["name"] = "name" }, WellKnownExpressions.EventSourceId)
                    }
                }
            }
        };
    }

    async Task Because() => _error = await Catch.Exception(async () => _projection = await _factory.Create(_eventStore, _namespace, _definition, _readModel, []));

    [Fact] void should_register_using_the_explicit_child_identifier() => _error.ShouldBeNull();
    [Fact] void should_keep_the_explicit_child_identifier() => _projection.ChildProjections.Single().IdentifiedByProperty.ShouldEqual((PropertyPath)"itemId");
    [Fact] void should_subscribe_to_the_child_join_event() => _projection.ChildProjections.Single().EventTypes.ShouldContain(new EventType("ItemRenamed", EventTypeGeneration.First));
}
