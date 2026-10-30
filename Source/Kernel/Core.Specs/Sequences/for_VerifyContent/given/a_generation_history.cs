// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Identities;

namespace Cratis.Chronicle.Sequences.for_VerifyContent.given;

public class a_generation_history : a_migrated_event
{
    protected StoredEventGenerations _history;
    protected Dictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    async Task Establish()
    {
        var version = EventTypeMigrationsVersion.For(_definition.Migrations);
        _versions = new() { [version] = _definition.Migrations.ToArray() };
        var sequence = new EventSequenceStorage("store", "tenant", "log", new IdentityStorage());
        var schema = _definition.Generations.First().Schema;
        var source = _converter.ToExpandoObject(JsonNode.Parse("""{"value":42}""")!.AsObject(), schema);
        await sequence.Append(EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, new("event", 1), CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new Dictionary<EventTypeGeneration, ExpandoObject> { [1] = source }, new Dictionary<EventTypeGeneration, EventHash>());
        var observed = (await sequence.GetStoredGenerations(EventSequenceNumber.First))!;
        var target = _converter.ToExpandoObject(JsonNode.Parse("""{"value":42,"detail":"default"}""")!.AsObject(), _definition.Generations.Last().Schema);
        (await sequence.TryAddGenerations(observed, [new(2, target, EventHash.NotSet, new(1, true, version))])).ShouldBeTrue();
        _history = (await sequence.GetStoredGenerations(EventSequenceNumber.First))!;
        using var cursor = await sequence.GetRange(EventSequenceNumber.First, EventSequenceNumber.First);
        (await cursor.MoveNext()).ShouldBeTrue();
        _stored = cursor.Current.Single();
        _storage.GetEventStore("store").GetNamespace("tenant").GetEventSequence("log").GetStoredGenerations(EventSequenceNumber.First).Returns(_ => _history);
        _storage.GetEventStore("store").EventTypes.GetMigrationsVersions("event").Returns(_ => _versions);
    }

    protected void ChangeMigrations(string upcast = """{"detail":{"$defaultValue":"changed"}}""", string downcast = "{}") =>
        _definition = _definition with { Migrations = [new(1, 2, [], JsonNode.Parse(upcast)!.AsObject(), JsonNode.Parse(downcast)!.AsObject())] };

    protected void ForgetProvenance() => _history = _history with { DerivedGenerations = new Dictionary<EventTypeGeneration, GenerationProvenance>() };

    protected void RequestSecondGeneration(string content = """{"value":42,"detail":"default"}""") =>
        _command = _command with { EventType = new("event", 2, false), Content = content };
}
