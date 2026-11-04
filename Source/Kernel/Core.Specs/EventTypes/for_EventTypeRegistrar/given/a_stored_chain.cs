// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Schemas;
using MemoryStorage = Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class a_stored_chain : all_dependencies
{
    protected const string FirstSchema = """{"type":"object","properties":{"one":{"type":"string"}}}""";
    protected const string SecondSchema = """{"type":"object","properties":{"two":{"type":"string"}}}""";
    protected const string ThirdSchema = """{"type":"object","properties":{"three":{"type":"string"}}}""";
    protected MemoryStorage _memory;
    protected Concepts.Events.EventTypeDefinition _chain;
    protected EventTypeRegistration _request;
    protected EventTypeMigrationsVersion _version;

    async Task Establish()
    {
        _memory = new();
        _chain = StoredEventType("retained-chain", (1, FirstSchema), (2, SecondSchema), (3, ThirdSchema)) with
        {
            Migrations = [new(1, 2, [], new JsonObject { ["two"] = "one" }, new JsonObject { ["one"] = "two" }), new(2, 3, [], new JsonObject { ["three"] = "two" }, new JsonObject { ["two"] = "three" })]
        };
        foreach (var generation in _chain.Generations)
        {
            generation.Schema.EnsureComplianceMetadata();
        }
        await _memory.Register([new EventTypeToRegister(_chain, Concepts.Events.EventTypeSource.Code)]);
        _version = EventTypeMigrationsVersion.For(_chain.Migrations);
        await _memory.RecordMigrationsVersion(_chain.Id, _version, _chain.Migrations);
        _eventTypesStorage.GetAllDefinitions().Returns(_ => _memory.GetAllDefinitions());
        _eventTypesStorage.GetDefinition(Arg.Any<EventTypeId>()).Returns(call => _memory.GetDefinition(call.Arg<EventTypeId>()));
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(call => _memory.Register(call.Arg<IEnumerable<EventTypeToRegister>>()));
        _eventTypesStorage.RecordMigrationsVersion(Arg.Any<EventTypeId>(), Arg.Any<EventTypeMigrationsVersion>(), Arg.Any<IEnumerable<Concepts.Events.EventTypeMigrationDefinition>>()).Returns(call => _memory.RecordMigrationsVersion(call.Arg<EventTypeId>(), call.Arg<EventTypeMigrationsVersion>(), call.Arg<IEnumerable<Concepts.Events.EventTypeMigrationDefinition>>()));
        _eventTypesStorage.ClearReceivedCalls();
        _request = new()
        {
            Type = new() { Id = _chain.Id.Value, Generation = 2 },
            Owner = Contracts.Events.EventTypeOwner.Client,
            Source = Contracts.Events.EventTypeSource.Code,
            Schema = SecondSchema,
            Generations = [new() { Generation = 1, Schema = FirstSchema }, new() { Generation = 2, Schema = SecondSchema }],
            Migrations = [new() { FromGeneration = 1, ToGeneration = 2, UpcastJmesPath = "{\"two\":\"one\"}", DowncastJmesPath = "{\"one\":\"two\"}" }]
        };
    }

    protected Task Register() => _subject.Register("test-store", [_request], false, _storage, _eventTypesCacheClient, _patternCapture);

    void Destroy() => _memory.Dispose();
}
