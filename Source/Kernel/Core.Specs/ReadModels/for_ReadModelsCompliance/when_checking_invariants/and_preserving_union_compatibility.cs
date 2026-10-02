// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.when_checking_invariants;

public class and_preserving_union_compatibility
{
    public static TheoryData<string, string, bool, bool> Cells
    {
        get
        {
            var cells = new TheoryData<string, string, bool, bool>();
            var combinations = from union in new[] { "anyOf", "oneOf" }
                               from shape in new[] { "root", "nested", "sibling", "whole" }
                               from pii in new[] { false, true }
                               from erased in new[] { false, true }
                               where shape != "whole" || pii
                               select (union, shape, pii, erased);
            foreach (var (union, shape, pii, erased) in combinations)
            {
                cells.Add(union, shape, pii, erased);
            }
            return cells;
        }
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public async Task should_preserve_values_and_availability_through_every_boundary(string union, string shape, bool pii, bool erased)
    {
        const string Marker = "\"compliance\":[{\"metadataType\":\"PII\",\"details\":\"\"}]";
        var alternatives = JsonNode.Parse("""[{"type":"object","properties":{"name":{"type":"string"}}},{"type":"null"}]""")!;
        var root = new JsonObject();
        ExpandoObject state;
        if (shape == "whole")
        {
            alternatives[1] = new JsonObject { ["$ref"] = "#/$defs/nil" };
            root["$defs"] = new JsonObject { ["nil"] = new JsonObject { ["type"] = "null" } };
            root["type"] = "object";
            var member = JsonNode.Parse($$"""{ {{Marker}} }""")!.AsObject();
            member[union] = alternatives;
            root["properties"] = new JsonObject { ["value"] = member };
            state = given.compliance_matrix.State(("value", given.compliance_matrix.State(("name", "Alice"), ("extra", "retained"))));
        }
        else if (shape == "root")
        {
            root[union] = alternatives;
            state = given.compliance_matrix.State(("name", "Alice"), ("extra", "retained"));
        }
        else if (shape == "nested")
        {
            root["type"] = "object";
            root["properties"] = new JsonObject { ["value"] = new JsonObject { [union] = alternatives } };
            var value = union == "oneOf"
                ? given.compliance_matrix.State(("name", "Alice"), ("extra", "retained"))
                : given.compliance_matrix.State(("name", "Alice"));
            state = given.compliance_matrix.State(("value", value));
        }
        else
        {
            root["type"] = "object";
            root["properties"] = new JsonObject { ["name"] = new JsonObject { ["type"] = "string" } };
            root[union] = JsonNode.Parse("""[{"properties":{"value":{"type":"string"}}},{"properties":{"other":{"type":"integer"}}}]""");
            state = given.compliance_matrix.State(("name", "Alice"));
        }
        if (pii && shape != "whole")
        {
            // For a root union, put the entire plain union beside a protected member. No branch selection
            // may narrow that plain value just because another member needs protection.
            if (shape == "root")
            {
                root = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { ["value"] = root } };
                state = given.compliance_matrix.State(("value", state));
                if (union == "anyOf") ((IDictionary<string, object?>)((IDictionary<string, object?>)state)["value"]!).Remove("extra");
            }
            root["properties"]!["guard"] = JsonNode.Parse($$"""{"type":"string",{{Marker}} }""");
            ((IDictionary<string, object?>)state)["guard"] = "private";
        }
        var protectedName = shape == "whole" ? "value" : "guard";
        var schema = await JsonSchema.FromJsonAsync(root.ToJsonString());
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var input = JsonSerializer.SerializeToNode(state)!.AsObject();
        var converted = converter.ToJsonObject(converter.ToExpandoObject(input, schema), schema);
        Assert.True(JsonNode.DeepEquals(input, converted), "I3: conversion must preserve main-compatible union values");
        var encryption = new Encryption();
        var keys = new InMemoryEncryptionKeyStorage();
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption)), NullLogger<JsonSchemaMetadataManager>.Instance);
        var compliance = new ReadModelsCompliance(manager, converter);
        if (erased) await keys.RecordErasureFor("store", "Default", "subject");

        var appendError = await Catch.Exception(async () =>
        {
            var appended = await manager.Apply("store", "Default", schema, "subject", input);
            CheckStored(appended);
            CheckReleased(await manager.Release("store", "Default", schema, "subject", appended));
        });
        if (pii && erased) Assert.IsType<SchemaMetadataActionFailed>(appendError);
        else Assert.Null(appendError);

        var applied = await compliance.Apply("store", "Default", schema, "subject", state);
        CheckStored(JsonSerializer.SerializeToNode(applied)!.AsObject());
        CheckReleased(JsonSerializer.SerializeToNode(await compliance.Release("store", "Default", schema, applied))!.AsObject());
        CheckReleased(await compliance.ReleaseJson("store", "Default", schema, JsonSerializer.SerializeToNode(applied)!.AsObject()));
        var reduced = await Reduce(compliance, schema, state);
        CheckStored(JsonSerializer.SerializeToNode(reduced)!.AsObject());
        CheckReleased(JsonSerializer.SerializeToNode(await compliance.Release("store", "Default", schema, reduced))!.AsObject());
        if (erased) (await keys.HasFor("store", "Default", "subject")).ShouldBeFalse();

        void CheckStored(JsonObject stored)
        {
            if (pii)
            {
                var value = stored[protectedName]!.GetValue<string>();
                Assert.True(erased ? value.Length == 0 : ProtectedValueCodec.TryDecodeCipherText(encryption, value, out _), "I1: protected values cannot be stored in cleartext");
            }
            CheckPlain(stored);
        }
        void CheckReleased(JsonObject released)
        {
            if (pii)
            {
                var placeholder = shape == "whole" ? (JsonNode)new JsonObject() : JsonValue.Create(string.Empty);
                var expected = erased ? placeholder : input[protectedName];
                Assert.True(JsonNode.DeepEquals(expected, released[protectedName]), erased ? "I2: erased payload must stay erased" : "I3: whole protected payload must survive release");
            }
            CheckPlain(released);
        }
        void CheckPlain(JsonObject document)
        {
            foreach (var (name, value) in input.Where(_ => _.Key != protectedName && _.Key != "__subject"))
            {
                Assert.True(JsonNode.DeepEquals(value, document[name]), $"I3: {name} changed");
            }
        }
    }

    static async Task<ExpandoObject> Reduce(IReadModelsCompliance compliance, JsonSchema schema, ExpandoObject state)
    {
        var readModel = new ReadModelDefinition("union", "Union", "Union", ReadModelOwner.Client, ReadModelSource.Code, ReadModelObserverType.Reducer, "observer", new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB), new Dictionary<ReadModelGeneration, JsonSchema> { [(ReadModelGeneration)1] = schema }, []);
        var sink = Substitute.For<ISink>();
        sink.FindOrDefault(Arg.Any<Key>()).Returns(Task.FromResult<ExpandoObject?>(null));
        sink.ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>()).Returns(Task.FromResult(Enumerable.Empty<FailedPartition>()));
        ExpandoObject? stored = null;
        var observed = Substitute.For<IReadModelsCompliance>();
        observed.Apply(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<JsonSchema>(), Arg.Any<string>(), Arg.Any<ExpandoObject>())
            .Returns(async call => stored = await compliance.Apply(call.ArgAt<EventStoreName>(0), call.ArgAt<EventStoreNamespaceName>(1), call.ArgAt<JsonSchema>(2), call.ArgAt<string>(3), call.ArgAt<ExpandoObject>(4)));
        var pipeline = new ReducerPipeline(readModel, sink, new ObjectComparer(), observed, "store", "Default");
        var @event = new AppendedEvent(EventContext.From("store", "Default", EventType.Unknown, EventSourceType.Default, "subject", EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First, CorrelationId.NotSet), new ExpandoObject());
        await pipeline.Reduce(new ReducerContext([@event], new Key("subject", ArrayIndexers.NoIndexers)), (_, _) => Task.FromResult(new ReducerSubscriberResult(new ObserverSubscriberResult(ObserverSubscriberState.Ok, EventSequenceNumber.First, [], string.Empty), state)));
        await sink.Received(1).ApplyChanges(Arg.Any<Key>(), Arg.Any<IChangeset<AppendedEvent, ExpandoObject>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<SinkWriteMode>());
        return stored!;
    }
}
