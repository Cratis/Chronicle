// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeMigrations.given;

public class a_schema_filtered_chain : Specification
{
    protected static readonly EventStoreName Store = "store";
    protected IStorage _storage;
    protected ExpandoObjectConverter _converter;
    protected EventTypeMigrations _migrations;
    protected EventType _type;
    protected EventTypeDefinition _definition;
    protected JsonObject _source;
    protected IDictionary<EventTypeGeneration, ExpandoObject> _result;
    protected List<JsonObject> _inputs = [];
    protected List<JsonObject> _rawConversions = [];
    protected List<string> _callbacks = [];

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _converter = new(new TypeFormats());
        _migrations = new(_storage, _converter);
        _type = new("filtered-chain", 1);
        _source = JsonNode.Parse("""{"id":"1","status":"legacy"}""")!.AsObject();
        var full = Schema("""{"id":{"type":"string"},"status":{"type":["string","null"]}}""");
        var intermediate = Schema("""{"id":{"type":"string"}}""");
        _definition = new(
            _type.Id,
            EventTypeOwner.Server,
            false,
            [new(1, full), new(2, intermediate), new(3, full)],
            [new(1, 2, [], new JsonObject(), Default("status")), new(2, 3, [], Default("status"), new JsonObject())]);
        _storage.GetEventStore(Store).EventTypes.GetDefinition(_type.Id).Returns(_ => _definition);
    }

    protected static JsonSchema Schema(string properties) => JsonSchema.FromJson("{\"type\":\"object\",\"properties\":" + properties + "}");
    protected static JsonObject Default(string path) => new() { [path] = new JsonObject { ["$defaultValue"] = "pending" } };
    protected static IDictionary<string, object?> Members(ExpandoObject value) => value;
    protected void Converted(JsonObject raw, JsonSchema schema, ExpandoObject converted)
    {
        _rawConversions.Add((JsonObject)raw.DeepClone());
        _callbacks.Add("converted");
    }

    protected void Migrating(JsonObject operations, JsonObject content)
    {
        _inputs.Add((JsonObject)content.DeepClone());
        _callbacks.Add("migrating");
    }
}
