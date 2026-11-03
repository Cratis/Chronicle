// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventTypes.for_EventTypeRegistrar.when_registering;

public class and_a_bridge_edit_records_the_effective_chain : given.a_stored_chain
{
    EventTypeDefinition _effective;
    IReadOnlyDictionary<EventTypeMigrationsVersion, IEnumerable<EventTypeMigrationDefinition>> _versions;

    void Establish() => _request.Migrations[0].UpcastJmesPath = "{\"two\":\"one || one\"}";

    async Task Because()
    {
        await Register();
        _effective = await _memory.GetDefinition(_chain.Id);
        _versions = await _memory.GetMigrationsVersions(_chain.Id);
    }

    [Fact] void should_keep_both_bridges() => _effective.Migrations.Count().ShouldEqual(2);
    [Fact] void should_record_the_complete_effective_version() => EventTypeMigrationsVersion.For(_versions[EventTypeMigrationsVersion.For(_effective.Migrations)]).ShouldEqual(EventTypeMigrationsVersion.For(_effective.Migrations));
    [Fact] void should_keep_the_old_version_immutable() => EventTypeMigrationsVersion.For(_versions[_version]).ShouldEqual(_version);
    [Fact] async Task should_record_once() => await _eventTypesStorage.Received(1).RecordMigrationsVersion(_chain.Id, EventTypeMigrationsVersion.For(_effective.Migrations), Arg.Any<IEnumerable<EventTypeMigrationDefinition>>());
    [Fact] async Task should_invalidate_once() => await _eventTypesCacheClient.Received(1).Invalidate(Arg.Is<EventStoreName>(_ => _.Value == "test-store"), _chain.Id);
    [Fact] async Task should_resubscribe_once() => await _patternCapture.Received(1).SubscribeAcrossNamespaces(Arg.Any<EventStoreName>());
}
