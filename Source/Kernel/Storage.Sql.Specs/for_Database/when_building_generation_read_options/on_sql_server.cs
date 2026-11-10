// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_building_generation_read_options;

public class on_sql_server : Specification
{
    ServiceProvider _services;
    IEnumerable<int> _retriedErrors;

    void Establish()
    {
        var storage = new Configuration.Storage { ConnectionDetails = "Server=database,1433;Database=chronicle;User Id=sa;Password=Pa55word!;TrustServerCertificate=True" };
        _services = new ServiceCollection().BuildServiceProvider();
        var database = new Database(
            _services,
            Options.Create(new ChronicleOptions { Storage = storage }),
            Substitute.For<IEventSequenceMigrator>(),
            Substitute.For<IUniqueConstraintMigrator>(),
            Substitute.For<IReadModelMigrator>());
        using var context = new ClusterDbContext(database.ReadOptions<ClusterDbContext>(new(storage.ConnectionDetails, string.Empty, null)));
        var strategy = context.Database.CreateExecutionStrategy();
        _retriedErrors = (IEnumerable<int>)strategy.GetType().GetProperty("AdditionalErrorNumbers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(strategy)!;
    }

    void Destroy() => _services.Dispose();

    [Fact] void should_retry_a_login_refused_by_a_database_that_is_still_coming_online() => _retriedErrors.ShouldContain(18456);
}
