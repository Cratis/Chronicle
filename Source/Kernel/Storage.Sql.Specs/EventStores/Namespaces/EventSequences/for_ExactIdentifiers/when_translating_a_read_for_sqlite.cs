// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_a_read_for_sqlite() : given.a_storage_that_captures_statements(false)
{
    string _bySource;
    string _byStream;
    string _inFlight;

    async Task Because()
    {
        await ReadBySource("Alice");
        _bySource = Only();
        _captured.Clear();
        await ReadByStream("Stream", "Id");
        _byStream = Only();
        _captured.Clear();
        await RemoveFromInFlight(new("Alice", Chronicle.Properties.ArrayIndexers.NoIndexers));
        _inFlight = Only();
    }

    [Fact] void should_not_collate_the_event_source_id_comparison() => _bySource.ShouldNotContain("COLLATE");
    [Fact] void should_not_measure_the_event_source_id_length() => _bySource.ShouldNotContain("DATALENGTH");
    [Fact] void should_not_collate_the_stream_comparison() => _byStream.ShouldNotContain("COLLATE");
    [Fact] void should_not_measure_the_stream_length() => _byStream.ShouldNotContain("DATALENGTH");
    [Fact] void should_not_collate_the_partition_comparison() => _inFlight.ShouldNotContain("COLLATE");
    [Fact] void should_not_measure_the_partition_length() => _inFlight.ShouldNotContain("DATALENGTH");
}
