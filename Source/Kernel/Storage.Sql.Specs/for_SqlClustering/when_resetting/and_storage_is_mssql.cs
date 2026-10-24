// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;
#endif
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_SqlClustering.when_resetting;

public class and_storage_is_mssql : Specification
{
    IDatabase _database;
    SqlKernelStateResetHandler _handler;
    IOptions<ChronicleOptions> _options;
    Exception? _exception;

    void Establish()
    {
        _database = Substitute.For<IDatabase>();
        _options = Options.Create(new ChronicleOptions
        {
            Storage = new() { Type = StorageType.MsSql, ConnectionDetails = "Server=localhost;Database=chronicle;TrustServerCertificate=True" },
            Clustering = new() { Type = ClusteringType.Sql }
        });
        _handler = new SqlKernelStateResetHandler(_options, _database);
    }

    async Task Because() => _exception = await Catch.Exception(_handler.Reset);

    [Fact] void should_explain_why_reset_is_refused() => _exception.ShouldBeOfExactType<SqlClusteringPreventsKernelStateReset>();
#if DEVELOPMENT
    [Fact] void should_not_start_wiping() => _database.DidNotReceive().Wipe();

    [Fact] async Task should_also_refuse_direct_database_wipes()
    {
        await using var services = new ServiceCollection().BuildServiceProvider();
        var database = new Database(services, _options, Substitute.For<IEventSequenceMigrator>(), Substitute.For<IUniqueConstraintMigrator>(), Substitute.For<IReadModelMigrator>());
        (await Catch.Exception(database.Wipe)).ShouldBeOfExactType<SqlClusteringPreventsKernelStateReset>();
    }
#endif
}
