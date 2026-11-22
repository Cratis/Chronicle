// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Storage.EventTypes;
using EventTypeMigrationDefinition = Cratis.Chronicle.Concepts.Events.EventTypeMigrationDefinition;
using MemoryStorage = Cratis.Chronicle.Storage.InMemory.Events.EventTypes.EventTypesStorage;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.given;

public class a_batch_with_conditional_conflicts : all_dependencies
{
    protected const string Before = "synthetic-before";
    protected const string Conflict = "synthetic-conflict";
    protected const string After = "synthetic-after";
    protected const string OtherConflict = "synthetic-other-conflict";
    protected MemoryStorage _memory;
    protected EventTypeRegistration[] _batch;
    protected string[] _conflictingIds;
    protected Exception? _error;

    void Establish()
    {
        _memory = new();
        _eventTypesStorage.GetAllDefinitions().Returns(_ => _memory.GetAllDefinitions());
        _eventTypesStorage.GetDefinition(Arg.Any<EventTypeId>()).Returns(call => _memory.GetDefinition(call.Arg<EventTypeId>()));
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns(call => _memory.Register(call.Arg<IEnumerable<EventTypeToRegister>>()));
        _eventTypesStorage.RegisterIfAbsent(Arg.Any<EventTypeToRegister>()).Returns(call => RegisterConditionally(call.Arg<EventTypeToRegister>()));
        _eventTypesStorage.RecordMigrationsVersion(Arg.Any<EventTypeId>(), Arg.Any<EventTypeMigrationsVersion>(), Arg.Any<IEnumerable<EventTypeMigrationDefinition>>())
            .Returns(call => _memory.RecordMigrationsVersion(call.Arg<EventTypeId>(), call.Arg<EventTypeMigrationsVersion>(), call.Arg<IEnumerable<EventTypeMigrationDefinition>>()));
    }

    protected async Task RegisterBatch() => _error = await Catch.Exception(() =>
        _subject.Register("example-store", _batch, false, _storage, _eventTypesCacheClient, _patternCapture));

    protected static EventTypeRegistration Request(string id) => new()
    {
        Type = new() { Id = id, Generation = 1 },
        Owner = Contracts.Events.EventTypeOwner.Client,
        Source = Contracts.Events.EventTypeSource.Code,
        Schema = """{"type":"object","properties":{"value":{"type":"string"}}}"""
    };

    async Task<EventTypeRegistrationResult> RegisterConditionally(EventTypeToRegister registration)
    {
        if (_conflictingIds.Contains(registration.Definition.Id.Value))
        {
            var winner = registration with
            {
                Definition = StoredEventType(registration.Definition.Id.Value, (1, """{"type":"object","properties":{"value":{"type":"integer"}}}"""))
            };
            await _memory.RegisterIfAbsent(winner);
        }

        return await _memory.RegisterIfAbsent(registration);
    }

    void Destroy() => _memory.Dispose();
}
