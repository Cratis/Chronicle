// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_migrations_change_records_version : given.all_dependencies
{
    EventTypeDefinition _definition;

    void Establish()
    {
        _definition = StoredEventType("changed-event", (1, "{}"), (2, "{}")) with
        {
            Migrations = [new Concepts.Events.EventTypeMigrationDefinition(1, 2, [], new JsonObject { ["value"] = "changed" }, new JsonObject())]
        };
        _eventTypesStorage.Register(Arg.Any<IEnumerable<EventTypeToRegister>>()).Returns([_definition.Id]);
        _eventTypesStorage.GetDefinition(_definition.Id).Returns(_definition);
    }

    async Task Because() => await _subject.Register(
        "test-store",
        [new EventTypeRegistration { Type = new() { Id = "changed-event", Generation = 1 }, Schema = "{}" }],
        false,
        _storage,
        _eventTypesCacheClient,
        _patternCapture);

    [Fact] async Task should_record_the_stored_definition_version() => await _eventTypesStorage.Received(1).RecordMigrationsVersion(_definition.Id, EventTypeMigrationsVersion.For(_definition.Migrations), _definition.Migrations);
    [Fact] async Task should_record_it_in_the_registration_store() => await _eventTypesCacheClient.Received(1).Invalidate(Arg.Is<EventStoreName>(_ => _.Value == "test-store"), _definition.Id);
}
