// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

[Collection(PostgreSqlCollection.Name)]
public class and_the_event_has_a_single_write_of_a_direct_insert_on_postgresql(PostgreSqlFixture fixture) : and_the_event_has_a_single_write_of_a_direct_insert<PostgreSqlSinkHarness>
{
    string _metadataPreparation;

    protected override PostgreSqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Fixture = fixture, Interceptors = [capture] };

    protected override async Task Seed()
    {
        // A scalar child lookup validates provenance, then returns without opening or migrating the payload table.
        await _sink.TryFindRootKeyByChildValue("group", "fingerprint-warmup");
        _metadataPreparation = string.Join('\n', _capture.Commands);
    }

    protected override int RecordedCommands => 6;

    protected override string RecordedFingerprint => "a620666fe1640afa00f3df32793a0dc865c6266efb6f0a2deda13b182fd3e4e6";

    [Fact] void should_prepare_only_the_provenance_table_outside_the_measured_write() =>
        _metadataPreparation.ShouldEqual("CREATE SCHEMA IF NOT EXISTS \"public\"; CREATE TABLE IF NOT EXISTS \"public\".\"__cratis_sink_provenance\" (\"Id\" varchar(200) PRIMARY KEY, \"Version\" integer NOT NULL, \"Restricted\" boolean NOT NULL)");
}
