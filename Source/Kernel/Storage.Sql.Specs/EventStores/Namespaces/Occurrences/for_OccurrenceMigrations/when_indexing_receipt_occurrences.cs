// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.for_OccurrenceMigrations;

public class when_indexing_receipt_occurrences : Specification
{
    string _id;
    string _previous;
    Type _context;
    IReadOnlyList<MigrationOperation> _operations;

    void Because()
    {
        _id = typeof(v19_67_1).GetCustomAttribute<MigrationAttribute>()!.Id;
        _previous = typeof(v19_67_0).GetCustomAttribute<MigrationAttribute>()!.Id;
        _context = typeof(v19_67_1).GetCustomAttribute<DbContextAttribute>()!.ContextType;
        _operations = new v19_67_1 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations;
    }

    [Fact] void should_use_the_namespace_prefix() => _id.ShouldEqual("NS-Occurrences-v19_67_1");
    [Fact] void should_bind_the_namespace_context() => _context.ShouldEqual(typeof(NamespaceDbContext));
    [Fact] void should_sort_after_receipt_infrastructure() => string.CompareOrdinal(_id, _previous).ShouldBeGreaterThan(0);
    [Fact] void should_add_the_occurrence_column() => _operations.OfType<AddColumnOperation>().Single().Name.ShouldEqual("OccurrenceId");
    [Fact] void should_index_the_occurrence_column() => _operations.OfType<CreateIndexOperation>().Single().Columns.ShouldContainOnly("OccurrenceId");
    [Fact] void should_index_the_shared_receipt_table() => _operations.OfType<CreateIndexOperation>().Single().Table.ShouldEqual(WellKnownTableNames.EffectReceipts);
}
