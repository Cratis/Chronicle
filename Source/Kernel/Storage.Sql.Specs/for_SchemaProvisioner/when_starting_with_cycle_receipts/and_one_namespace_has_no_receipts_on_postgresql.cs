// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.when_starting_with_cycle_receipts;

[Collection(PostgreSqlCollection.Name)]
public class and_one_namespace_has_no_receipts_on_postgresql(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    Exception _error;

    async Task Establish()
    {
        var provisioner = new SchemaProvisioner(Database, NullLogger<SchemaProvisioner>.Instance);
        await provisioner.Migrate(new([Store], ["production", "staging"]));
        var name = await Binding(DatabaseKind.ReadModels, Store, "staging");
        await Execute(ConnectionFor(name), "DROP TABLE chronicle_sink_cycle_receipts");
    }

    async Task Because()
    {
        await using var services = for_SchemaProvisioner.given.schema_services.Create(new ChronicleOptions
        {
            Storage = new() { Type = StorageType.PostgreSql, ConnectionDetails = ConnectionString, SchemaMode = SchemaMode.Verify }
        });
        var database = services.GetRequiredService<Database>();
        _error = await Catch.Exception(() => new ClusterDbContextMigrator(database).Execute(CancellationToken.None));
    }

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<SchemaNotCurrent>();
}
