// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.for_RegistrationIntentsMigration;

public class when_adding_registration_intents : Specification
{
    string _id;
    Type _context;
    string[] _tables;
    string[] _columns;

    void Because()
    {
        var migration = new Migrations.v20_1_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" };
        _id = typeof(Migrations.v20_1_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _context = typeof(Migrations.v20_1_0).GetCustomAttribute<DbContextAttribute>()!.ContextType;
        var table = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
        _tables = [.. table.Select(_ => _.Name)];
        _columns = [.. table.SelectMany(_ => _.Columns).Select(_ => _.Name)];
    }

    [Fact] void should_name_the_release() => _id.ShouldEqual("ES-RegistrationIntents-v20_1_0");
    [Fact] void should_bind_the_event_store_context() => _context.ShouldEqual(typeof(EventStoreDbContext));
    [Fact] void should_add_only_the_registration_intents_table() => _tables.ShouldContainOnly(WellKnownTableNames.RegistrationIntents);
    [Fact] void should_have_a_column_for_every_property_of_the_row() => _columns.ShouldContainOnly(typeof(RegistrationIntentEntry).GetProperties().Select(_ => _.Name));
}
