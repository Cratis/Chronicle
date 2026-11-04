// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_metadata_only_registration_retains_the_version : given.a_stored_chain
{
    void Establish()
    {
        _request.Type.Generation = 1;
        _request.Schema = FirstSchema;
        _request.Generations = [];
        _request.Migrations = [];
        _request.EventStore = "origin-changed";
    }

    Task Because() => Register();

    [Fact] async Task should_record_the_existing_full_chain() => await _eventTypesStorage.Received(1).RecordMigrationsVersion(_chain.Id, _version, Arg.Is<IEnumerable<EventTypeMigrationDefinition>>(_ => _.Count() == 2));
    [Fact] async Task should_not_add_an_empty_version() => (await _memory.GetMigrationsVersions(_chain.Id)).Count.ShouldEqual(1);
}
