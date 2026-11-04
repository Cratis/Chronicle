// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql_and_the_namespace_was_never_provisioned(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    DbContextScope<NamespaceDbContext>? _scope;
    IEnumerable<ReadModelOccurrence> _occurrences;
    string[] _databasesBefore;
    string[] _databasesAfter;
    int _bindingsAfter;

    async Task Establish()
    {
        await using var cluster = await Database.Cluster();
        _databasesBefore = await PhysicalDatabases();
    }

    async Task Because()
    {
        _scope = await Database.ExistingNamespace(Store, Namespace);
        _occurrences = await new ReplayedModelsStorage(Store, Namespace, Database).GetOccurrences("model");
        _databasesAfter = await PhysicalDatabases();
        _bindingsAfter = await BindingCount();
    }

    [Fact] void should_have_provisioned_the_cluster_database_in_the_setup() => _databasesBefore.ShouldContain(new Npgsql.NpgsqlConnectionStringBuilder(ConnectionString).Database);
    [Fact] void should_report_the_namespace_as_absent() => _scope.ShouldBeNull();
    [Fact] void should_have_no_occurrences() => _occurrences.ShouldBeEmpty();
    [Fact] void should_not_create_a_database() => string.Join(',', _databasesAfter).ShouldEqual(string.Join(',', _databasesBefore));
    [Fact] void should_not_bind_a_database_name() => _bindingsAfter.ShouldEqual(0);
}
