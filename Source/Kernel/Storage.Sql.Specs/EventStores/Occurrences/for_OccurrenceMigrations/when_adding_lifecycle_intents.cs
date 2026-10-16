// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrenceMigrations;

public class when_adding_lifecycle_intents : Specification
{
    string _id;
    string _previous;
    Type _context;
    string[] _tables;

    void Because()
    {
        _id = typeof(Migrations.v19_70_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _previous = typeof(Migrations.v19_69_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _context = typeof(Migrations.v19_70_0).GetCustomAttribute<DbContextAttribute>()!.ContextType;
        _tables = new Migrations.v19_70_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations.OfType<CreateTableOperation>().Select(table => table.Name).ToArray();
    }

    [Fact] void should_name_the_release() => _id.ShouldEqual("ES-Occurrences-v19_70_0");
    [Fact] void should_bind_the_event_store_context() => _context.ShouldEqual(typeof(EventStoreDbContext));
    [Fact] void should_sort_after_trigger_definitions() => string.CompareOrdinal(_id, _previous).ShouldBeGreaterThan(0);
    [Fact] void should_add_only_lifecycle_intents() => _tables.ShouldContainOnly("LifecycleFanOutIntents");
}
