// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_storage_round_trip : a_stored_event
{
    protected const string EnumSchema = """
        {"type":"object","properties":{
          "flags":{"type":"integer","enum":[0,1,2],"x-enumNames":["None","Read","Write"]},
          "status":{"type":"integer","enum":[0,1,2],"x-enumNames":["None","Read","Write"]}}}
        """;
    protected const string DecimalSchema = """{"type":"object","properties":{"value":{"type":"number","format":"decimal"}}}""";
    protected const string OffsetSchema = """{"type":"object","properties":{"value":{"type":"string","format":"date-time-offset"}}}""";

    [Flags]
    protected enum Access
    {
        None = 0,
        Read = 1,
        Write = 2
    }

    protected static string NonMemberEnums => JsonSerializer.Serialize(new { flags = Access.Read | Access.Write, status = (Access)8 });

    protected async Task Store(string schemaJson, string content, bool sql)
    {
        var schema = await JsonSchema.FromJsonAsync(schemaJson);
        _command = _command with { Content = content };
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, schema));
        var generations = new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = _converter.ToExpandoObject(JsonNode.Parse(content)!.AsObject(), schema) };
        if (sql)
        {
            _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log")
                .SerializeContentForVerification(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
                .Returns(call => EventEntryConverter.SerializeContent(call.Arg<ExpandoObject>()));
            var entry = EventEntryConverter.ToEventEntry(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, generations);
            _stored = await EventEntryConverter.ToAppendedEvent(entry, "store", "tenant", Substitute.For<IIdentityStorage>());
        }
        else
        {
            var sequence = new Storage.InMemory.EventSequences.EventSequenceStorage("store", "tenant", "log", new Storage.InMemory.Identities.IdentityStorage());
            var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, generations, new Dictionary<EventTypeGeneration, EventHash>());
            appended.IsSuccess.ShouldBeTrue();
            using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
            (await cursor.MoveNext()).ShouldBeTrue();
            _stored = cursor.Current.Single();
        }
    }
}
