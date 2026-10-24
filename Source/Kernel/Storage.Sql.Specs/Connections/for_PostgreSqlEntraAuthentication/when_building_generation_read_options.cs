// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_PostgreSqlEntraAuthentication;

public class when_building_generation_read_options : Specification
{
    ServiceProvider _services;
    SqlConnectionResolution _resolution;
    NpgsqlDataSource _source;
    Exception? _exception;

    void Establish()
    {
        var storage = new Configuration.Storage
        {
            ConnectionDetails = "Host=postgres.example;Database=chronicle;Username=kernel",
            PostgreSqlEntra = new() { Enabled = true }
        };
        _services = new ServiceCollection().BuildServiceProvider();
        _resolution = SqlConnectionResolution.From(_services, storage);
        _services.Dispose();
        _services = new ServiceCollection().AddSingleton(_resolution).BuildServiceProvider();
        var database = new Database(
            _services,
            Options.Create(new ChronicleOptions { Storage = storage }),
            Substitute.For<IEventSequenceMigrator>(),
            Substitute.For<IUniqueConstraintMigrator>(),
            Substitute.For<IReadModelMigrator>());
        var options = database.ReadOptions<ClusterDbContext>(new(storage.ConnectionDetails, string.Empty, null));
        var builder = new NpgsqlDataSourceBuilder(storage.ConnectionDetails);
#pragma warning disable EF1001 // Inspect the provider configuration without opening a database connection.
        options.FindExtension<NpgsqlOptionsExtension>()!.DataSourceBuilderAction!(builder);
#pragma warning restore EF1001
        _source = builder.Build();
    }

    void Because() => _exception = Catch.Exception(() => _source.Password = "manual-password");

    void Destroy()
    {
        _source.Dispose();
        _resolution.Dispose();
        _services.Dispose();
    }

    [Fact] void should_build_a_data_source_with_a_password_provider() => _exception.ShouldBeOfExactType<NotSupportedException>();
}
