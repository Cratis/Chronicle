// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_EventStoreNamespaceStorage;

public class when_only_operational_tables_exist : given.a_namespace_database
{
    string _before;
    bool _hasData;

    async Task Establish()
    {
        await Execute(NamespaceConnection, "CREATE TABLE EventSequences (Id TEXT); INSERT INTO EventSequences VALUES ('event-log'); CREATE TABLE Observers (Id TEXT); INSERT INTO Observers VALUES ('observer'); CREATE TABLE Occurrences (Id TEXT); INSERT INTO Occurrences VALUES ('occurrence'); CREATE TABLE __EFMigrationsHistory (MigrationId TEXT); INSERT INTO __EFMigrationsHistory VALUES ('old');");
        _before = await Catalog(NamespaceConnection);
    }

    async Task Because() => _hasData = await HasData();

    [Fact] void should_not_treat_operational_rows_or_stale_snapshots_as_events() => _hasData.ShouldBeFalse();
    [Fact] async Task should_not_migrate_or_change_the_schema() => (await Catalog(NamespaceConnection)).ShouldEqual(_before);
}
