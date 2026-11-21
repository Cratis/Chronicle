// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_events_exist_without_bootstrap_tables : given.a_namespace_database
{
    bool _hasData;
    string _before;

    async Task Establish()
    {
        await Execute(NamespaceConnection, "CREATE TABLE \"custom-events\" (sequence_number INTEGER, content TEXT); INSERT INTO \"custom-events\" VALUES (0, 'payload');");
        _before = await Catalog(NamespaceConnection);
    }

    async Task Because() => _hasData = await HasData();

    [Fact] void should_find_the_first_custom_event_without_sequence_snapshots() => _hasData.ShouldBeTrue();
    [Fact] async Task should_not_provision_bootstrap_tables() => (await Catalog(NamespaceConnection)).ShouldEqual(_before);
}
