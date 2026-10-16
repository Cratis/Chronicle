// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Occurrences.for_OccurrenceMigrations;

public class when_creating_namespace_tables : Specification
{
    string[] _tables;
    bool _mapsReceipts;

    void Because()
    {
        _tables = new Migrations.v19_67_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations.OfType<CreateTableOperation>().Select(table => table.Name).ToArray();
        using var context = new NamespaceDbContext(new DbContextOptionsBuilder<NamespaceDbContext>().UseSqlite("Data Source=:memory:").AddConceptAsSupport().Options);
        _mapsReceipts = context.Model.FindEntityType(typeof(EffectReceiptEntry)) is not null;
    }

    [Fact] void should_map_namespace_receipts() => _mapsReceipts.ShouldBeTrue();
    [Fact] void should_create_occurrences_and_namespace_receipts() => _tables.ShouldContainOnly(WellKnownTableNames.Occurrences, WellKnownTableNames.OccurrenceClaimFences, WellKnownTableNames.ScheduleDefinitions, WellKnownTableNames.ScheduleCursors, WellKnownTableNames.SkippedRanges, WellKnownTableNames.EffectReceipts);
}
