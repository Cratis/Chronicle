// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrationsAssembly;

public class when_listing_the_migrations_of_each_context : Specification
{
    readonly List<(Type Context, IMigrationsAssembly Assembly)> _assemblies = [];

    void Because()
    {
        Read(new ClusterDbContext(Options<ClusterDbContext>()));
        Read(new EventStoreDbContext(Options<EventStoreDbContext>()));
        Read(new NamespaceDbContext(Options<NamespaceDbContext>()));
    }

    [Fact] void should_use_the_release_ordered_assembly_for_every_context() =>
        _assemblies.ShouldEachConformTo(entry => entry.Assembly is ReleaseOrderedMigrationsAssembly);

    [Fact] void should_list_every_migration_declared_for_the_context() =>
        _assemblies.ShouldEachConformTo(entry => entry.Assembly.Migrations.Keys.Order().SequenceEqual(DeclaredFor(entry.Context).Order()));

    [Fact] void should_name_a_release_in_every_identifier() =>
        _assemblies.SelectMany(entry => entry.Assembly.Migrations.Keys).ShouldEachConformTo(id => MigrationReleaseOrder.TryGetRelease(id, out _));

    [Fact] void should_list_the_migrations_in_release_order() =>
        _assemblies.ShouldEachConformTo(entry =>
            entry.Assembly.Migrations.Keys.SequenceEqual(entry.Assembly.Migrations.Keys.Order(MigrationReleaseOrder.Instance)));

    static DbContextOptions<TContext> Options<TContext>()
        where TContext : DbContext =>
        Database.BuildOptions<TContext>(new ServiceCollection().BuildServiceProvider(), "Data Source=:memory:");

    static IEnumerable<string> DeclaredFor(Type context) =>
        typeof(ReleaseOrderedMigrationsAssembly).Assembly.DefinedTypes
            .Where(type => type.IsSubclassOf(typeof(Migration)) && type.GetCustomAttribute<DbContextAttribute>()?.ContextType == context)
            .Select(type => type.GetCustomAttribute<MigrationAttribute>()!.Id);

    void Read(DbContext context)
    {
        using (context)
        {
            _assemblies.Add((context.GetType(), context.GetService<IMigrationsAssembly>()));
        }
    }
}
