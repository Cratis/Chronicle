// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks.InMemory;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.Sinks.InMemory.for_InMemorySink.when_trying_to_find_root_key_by_child_value.given;

public class an_in_memory_sink : Specification
{
    protected InMemorySink _sink;

    void Establish()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Properties =
            {
                ["_id"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                ["children"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.Array,
                    Item = new JsonSchema
                    {
                        Type = JsonObjectType.Object,
                        Properties =
                        {
                            ["childId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
                        }
                    }
                },
                ["configurations"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.Array,
                    Item = new JsonSchema
                    {
                        Type = JsonObjectType.Object,
                        Properties =
                        {
                            ["configurationId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                            ["hubs"] = new JsonSchemaProperty
                            {
                                Type = JsonObjectType.Array,
                                Item = new JsonSchema
                                {
                                    Type = JsonObjectType.Object,
                                    Properties =
                                    {
                                        ["hubId"] = new JsonSchemaProperty { Type = JsonObjectType.String },
                                        ["name"] = new JsonSchemaProperty { Type = JsonObjectType.String }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var readModelDefinition = new ReadModelDefinition(
            "test-read-model",
            "TestReadModel",
            ReadModelOwner.Client,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, schema }
            },
            [],
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified);

        var typeFormats = Substitute.For<ITypeFormats>();
        _sink = new InMemorySink(readModelDefinition, typeFormats);
    }
}
