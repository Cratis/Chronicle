// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_in_one_transaction;

public class and_the_event_has_a_single_write_of_a_direct_insert_on_sqlite : and_the_event_has_a_single_write_of_a_direct_insert<SqlSinkHarness>
{
    string _metadataPreparation;

    protected override SqlSinkHarness CreateHarness(SqlCommandCapture capture) => new() { Interceptors = [capture] };

    protected override async Task Seed()
    {
        // Warm the one-time metadata preparation outside the measured write window.
        // Reading a missing row does not seed or migrate the payload table.
        await _sink.FindOrDefault(new Key("fingerprint-warmup", ArrayIndexers.NoIndexers));
        _metadataPreparation = string.Join('\n', _capture.Commands.Where(sql => sql.Contains(SinkProvenanceNames.Metadata, StringComparison.Ordinal)));
    }

    protected override int RecordedCommands => 5;

    protected override string RecordedFingerprint => "e3c50beaf1ac2cd5b313c516b20d4e144aadfe83ca66f0e31d124462c14eeb98";

    [Fact] void should_prepare_only_the_provenance_table_outside_the_measured_write() =>
        _metadataPreparation.ShouldEqual("CREATE TABLE IF NOT EXISTS \"__cratis_sink_provenance\" (\"Id\" TEXT NOT NULL PRIMARY KEY, \"Version\" INTEGER NOT NULL, \"Restricted\" INTEGER NOT NULL)");
}
