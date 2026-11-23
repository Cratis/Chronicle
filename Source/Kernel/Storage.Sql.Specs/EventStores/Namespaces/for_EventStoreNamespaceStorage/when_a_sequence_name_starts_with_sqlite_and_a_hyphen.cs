// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_a_sequence_name_starts_with_sqlite_and_a_hyphen : given.a_namespace_database
{
    bool _hasData;

    Task Establish() => Execute(NamespaceConnection, "CREATE TABLE \"sqlite-events\" (sequence_number INTEGER); INSERT INTO \"sqlite-events\" VALUES (0);");

    async Task Because() => _hasData = await HasData();

    [Fact] void should_not_confuse_the_sequence_with_a_sqlite_internal_table() => _hasData.ShouldBeTrue();
}
