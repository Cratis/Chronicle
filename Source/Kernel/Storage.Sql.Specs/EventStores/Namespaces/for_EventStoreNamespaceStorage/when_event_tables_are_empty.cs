// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_event_tables_are_empty : given.a_namespace_database
{
    bool _hasData;

    Task Establish() => Execute(NamespaceConnection, "CREATE TABLE \"event-log\" (sequence_number INTEGER); CREATE TABLE \"custom-events\" (sequence_number INTEGER);");

    async Task Because() => _hasData = await HasData();

    [Fact] void should_not_treat_empty_physical_tables_as_events() => _hasData.ShouldBeFalse();
}
