// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_ExactIdentifiers;

public class when_translating_a_read_for_sql_server() : given.a_storage_that_captures_statements(true)
{
    string _bySource;
    string _bySourceType;
    string _byDefaultSourceType;
    string _byStream;
    string _inFlight;

    async Task Because()
    {
        await ReadBySource("Alice");
        _bySource = Only();
        _captured.Clear();
        await ReadBySourceType("Account");
        _bySourceType = Only();
        _captured.Clear();
        await ReadBySourceType(Concepts.Events.EventSourceType.Default);
        _byDefaultSourceType = Only();
        _captured.Clear();
        await ReadByStream("Stream", "Id");
        _byStream = Only();
        _captured.Clear();
        await RemoveFromInFlight(new("Alice", Properties.ArrayIndexers.NoIndexers));
        _inFlight = Only();
    }

    [Fact] void should_seek_on_the_event_source_id_column() => _bySource.ShouldContain(SourceSeek);
    [Fact] void should_compare_the_event_source_id_in_binary_collation() => _bySource.ShouldContain("COLLATE Latin1_General_100_BIN2");
    [Fact] void should_compare_the_event_source_id_length_in_bytes() => _bySource.ShouldContain("DATALENGTH(");
    [Fact] void should_seek_on_the_event_source_type_column() => _bySourceType.ShouldContain(SourceTypeSeek);
    [Fact] void should_compare_the_event_source_type_in_binary_collation() => _bySourceType.ShouldContain("COLLATE Latin1_General_100_BIN2");
    [Fact] void should_compare_the_event_source_type_length_in_bytes() => _bySourceType.ShouldContain("DATALENGTH(");
    [Fact] void should_not_narrow_on_the_default_event_source_type() => _byDefaultSourceType.ShouldNotContain(SourceTypeSeek);
    [Fact]
    void should_seek_on_the_stream_type_and_stream_id_columns()
    {
        _byStream.ShouldContain(StreamTypeSeek);
        _byStream.ShouldContain(StreamIdSeek);
    }

    [Fact] void should_compare_the_stream_in_binary_collation() => _byStream.ShouldContain("COLLATE Latin1_General_100_BIN2");
    [Fact] void should_compare_the_stream_length_in_bytes() => _byStream.ShouldContain("DATALENGTH(");
    [Fact] void should_seek_on_the_partition_column() => _inFlight.ShouldContain(PartitionSeek);
    [Fact] void should_compare_the_partition_in_binary_collation() => _inFlight.ShouldContain("COLLATE Latin1_General_100_BIN2");
    [Fact] void should_compare_the_partition_length_in_bytes() => _inFlight.ShouldContain("DATALENGTH(");
}
