// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.for_Database.given;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

/// <summary>
/// The table has every column but the shadow that orders decimal keys, which a read used to recover by altering the table.
/// </summary>
public class and_a_sqlite_shadow_is_missing_with_connection_resolution : given.an_older_sqlite_occurrence
{
    protected override string Schema => given.ColdIndexedSink.DecimalKeySchema;

    protected override string OlderTableSql => $"CREATE TABLE \"{given.ColdIndexedSink.Occurrence}\" (\"id\" TEXT NOT NULL PRIMARY KEY, \"value\" INTEGER NULL, \"{WellKnownProperties.LastHandledEventSequenceNumber}\" INTEGER NULL, \"{WellKnownProperties.ReadModelInstanceInitialized}\" INTEGER NOT NULL DEFAULT 1)";

    protected override Database Reader => ResolvingDatabase();

    async Task Because()
    {
        _read = await ColdIndexedSink.ReadTheOccurrence(Reader, Schema);
        _catalogAfter = await Catalog(_readModelConnection);
        _filesAfter = string.Join(',', FilesOnDisk);
    }

    [Fact] void should_read_nothing_and_change_nothing() =>
        (_catalogBefore.Contains($"table:{ColdIndexedSink.Occurrence}:", StringComparison.Ordinal), _read, _catalogAfter == _catalogBefore && !_catalogAfter.Contains("__order_id", StringComparison.Ordinal), _filesAfter == _filesBefore).ShouldEqual((true, new OccurrenceRead(0, 0, 0), true, true));
}
