// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ContentComparison.given;

public class composed_storage_conversions : for_VerifyContent.given.a_mongodb_round_trip
{
    protected const string ComposedSchema = """
        {"allOf":[
          {"type":"object","properties":{"id":{"type":"integer"}}},
          {"type":"object","properties":{"value":{"type":"number"}}}
        ]}
        """;
    protected const string ReferencedSchema = """
        {"$ref":"#/definitions/composed","definitions":{"composed":{"allOf":[
          {"type":"object","properties":{"id":{"type":"integer"}}},
          {"$ref":"#/definitions/value"}
        ]},"value":{"type":"object","properties":{"value":{"type":"number"}}}}}
        """;
    protected JsonSchema _schema;
    protected IEventSequenceStorage[] _backends;
    protected JsonObject?[] _results;
    protected JsonObject?[] _controls;

    protected async Task Configure(string json)
    {
        await StoreInMongoDB(json, """{"id":1,"value":0.1}""");
        _schema = await JsonSchema.FromJsonAsync(json);
        var sql = Substitute.For<IEventSequenceStorage>();
        sql.SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(call => EventEntryConverter.SerializeContent(call.Arg<ExpandoObject>()));
        _backends = [
            new Storage.InMemory.EventSequences.EventSequenceStorage("store", "tenant", "log", new Storage.InMemory.Identities.IdentityStorage()),
            sql,
            _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")];
    }

    protected void Compare()
    {
        _results = _backends.Select(backend => ContentComparison.Prepare(JsonNode.Parse("""{"id":1,"value":0.10000000000000001}""")!.AsObject(), _schema, _converter, backend)).ToArray();
        _controls = _backends.Select(backend => ContentComparison.Prepare(JsonNode.Parse("""{"id":1,"value":0.1}""")!.AsObject(), _schema, _converter, backend)).ToArray();
    }
}
