// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_database_for_target_resolution : Specification
{
    protected const string Configured = "Host=pg.example;Database=chronicle;Username=app;Password=initial";
    protected ChronicleOptions _settings;
    protected ResolvedConnection _resolved;
    protected IStorageConnectionResolution _resolution;
    private protected DatabaseIdentity _identity;
    private protected Func<DatabaseIdentity, ConnectionPurpose, bool, Task<SqlTarget>> _target;
    private protected Func<Task<SqlTarget>> _clusterTarget;
    ServiceProvider _services;

    void Establish()
    {
        _settings = new() { Storage = new() { Type = StorageType.PostgreSql, ConnectionDetails = Configured } };
        _resolved = new(Database: "tenant", Schema: "initial");
        _identity = new(DatabaseKind.Namespace, "store", "tenant");
        _resolution = Substitute.For<IStorageConnectionResolution>();
        _resolution.IsConfigured.Returns(true);
        _resolution.Resolve(Arg.Any<ConnectionPurpose>(), Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>()).Returns(_ => _resolved);
        _resolution.RequestFor(Arg.Any<ConnectionPurpose>(), Arg.Any<StorageConnectionKind>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => StorageConnectionRequests.For(WellKnownConnectionStorage.PostgreSql, call.ArgAt<ConnectionPurpose>(0), call.ArgAt<StorageConnectionKind>(1), call.ArgAt<string>(2), call.ArgAt<string>(3)));
        _services = new ServiceCollection().AddSingleton(_resolution).BuildServiceProvider();
        var options = Substitute.For<IOptions<ChronicleOptions>>();
        options.Value.Returns(_ => _settings);
        var database = new Database(_services, options, Substitute.For<IEventSequenceMigrator>(), Substitute.For<IUniqueConstraintMigrator>(), Substitute.For<IReadModelMigrator>());
        _target = typeof(Database).GetMethod("Target", BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate<Func<DatabaseIdentity, ConnectionPurpose, bool, Task<SqlTarget>>>(database);
        _clusterTarget = () => _target(DatabaseIdentity.Cluster, ConnectionPurpose.Runtime, false);
    }

    void Destroy() => _services.Dispose();
}
