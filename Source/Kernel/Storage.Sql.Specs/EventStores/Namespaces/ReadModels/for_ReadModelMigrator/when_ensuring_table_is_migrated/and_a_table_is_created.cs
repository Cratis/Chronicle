// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_table_is_migrated;

public class and_a_table_is_created : given.a_read_model_migrator
{
    const string PrimaryTable = "books_primary";
    const string ReplayTable = "replay-books_primary";

    IReadOnlyDictionary<string, string> _formats;

    async Task Because()
    {
        IReadOnlyList<ProjectedColumn> columns =
        [
            new ProjectedColumn("Id", typeof(string), IsKey: true, IsJson: false, IsArray: false, IsNullable: false),
            new ProjectedColumn(WellKnownProperties.LastHandledEventSequenceNumber, typeof(long?), IsKey: false, IsJson: false, IsArray: false, IsNullable: true)
        ];

        await using var primary = CreateContext(PrimaryTable, columns);
        await _migrator.EnsureTableMigrated(PrimaryTable, primary);
        await using var replay = CreateContext(ReplayTable, columns);
        await _migrator.EnsureTableMigrated(ReplayTable, replay);

        await using var reader = CreateContext(PrimaryTable, columns);
        _formats = await reader.ReadModelKeyFormats.AsNoTracking().ToDictionaryAsync(entry => entry.TableName, entry => entry.Format);
    }

    [Fact] void should_record_a_primary_table_as_k1() => _formats[PrimaryTable].ShouldEqual("k1");
    [Fact] void should_record_a_replay_table_as_building_k1() => _formats[ReplayTable].ShouldEqual("building-k1");
}
