// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrenceMigrations;

public class when_creating_application_tables : Specification
{
    string[] _tables;
    bool _mapsReceipts;

    void Because()
    {
        _tables = new Migrations.v19_67_0 { ActiveProvider = "Microsoft.EntityFrameworkCore.Sqlite" }.UpOperations.OfType<CreateTableOperation>().Select(table => table.Name).ToArray();
        using var context = new EventStoreDbContext(new DbContextOptionsBuilder<EventStoreDbContext>().UseSqlite("Data Source=:memory:").AddConceptAsSupport().WithoutServiceProviderCaching().Options);
        _mapsReceipts = context.Model.FindEntityType(typeof(EffectReceiptEntry)) is not null;
    }

    [Fact] void should_not_map_namespace_receipts() => _mapsReceipts.ShouldBeFalse();
    [Fact] void should_create_only_application_occurrence_tables() => _tables.ShouldContainOnly(WellKnownTableNames.Occurrences, WellKnownTableNames.OccurrenceClaimFences, WellKnownTableNames.ScheduleDefinitions, WellKnownTableNames.ScheduleCursors, WellKnownTableNames.SkippedRanges);
}
