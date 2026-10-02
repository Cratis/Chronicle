// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.EventSequences.Migrations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_migrated_event : a_stored_event
{
    protected EventTypeDefinition _definition;

    async Task Establish()
    {
        var first = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer"}}}""");
        var second = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"value":{"type":"integer"},"detail":{"type":"string"}}}""");
        _definition = new(
            "event",
            EventTypeOwner.Client,
            false,
            [new(1, first), new(2, second)],
            [new(1, 2, [], JsonNode.Parse("""{"detail":{"$defaultValue":"default"}}""")!.AsObject(), new JsonObject())]);
        _storage.GetEventStore("store").EventTypes.HasFor("event", 2U).Returns(true);
        _storage.GetEventStore("store").EventTypes.GetFor("event", 1U).Returns(new EventTypeSchema(new("event", 1), EventTypeOwner.Client, EventTypeSource.Code, first));
        _storage.GetEventStore("store").EventTypes.GetFor("event", 2U).Returns(new EventTypeSchema(new("event", 2), EventTypeOwner.Client, EventTypeSource.Code, second));
        _storage.GetEventStore("store").EventTypes.Configure().GetDefinition("event").Returns(_ => _definition);
        await Append("""{"value":42,"detail":"default"}""");
    }

    protected async Task Append(string json)
    {
        var source = JsonNode.Parse(json)!.AsObject();
        var schema = _definition.Generations.Single(_ => _.Generation.Value == 2).Schema;
        var generations = await new EventTypeMigrations(_storage, _converter).MigrateToAllGenerations("store", new("event", 2), source, _converter.ToExpandoObject(source, schema));
        var sequence = new EventSequenceStorage("store", "tenant", "log", new IdentityStorage());
        var appended = await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 2), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, generations, new Dictionary<EventTypeGeneration, EventHash>());
        appended.IsSuccess.ShouldBeTrue();
        using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
    }
}
