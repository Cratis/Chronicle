// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsMigration;

[Collection(SqlServerCollection.Name)]
public class when_upgrading_on_sql_server(SqlServerFixture fixture) : for_ClosedStreamsConstraintStorage.given.a_closed_streams_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    List<string> _collations;
    List<int> _lengths;
    List<string> _rows;
    List<string> _rowsAfterSecondRun;
    List<string> _primaryKey;

    async Task Because()
    {
        await _harness.UpgradeFromEarlierLayout(
            "INSERT INTO [ClosedStreams] ([EventSequenceId], [StreamType], [StreamId], [EventSourceId], [EventSourceType], [Owner], [Dimensions], [SequenceNumber], [ClosedAt]) VALUES " +
            "(N'event-log', N'Orders', N'Alice', N'Alice', N'Default', N'owner', 15, 1, '2024-01-02T03:04:05+00:00'), " +
            "(N'event-log', N'Orders', N'bob ', N'bob ', N'Default', N'owner', 15, 2, NULL), " +
            "(N'event-log', N'Orders', N'carol' + NCHAR(1), N'carol' + NCHAR(1), N'Default', N'owner', 15, 3, NULL)");
        _collations = await _harness.Query<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'ClosedStreams') AND name IN ('StreamType', 'StreamId', 'EventSourceId', 'EventSourceType')");
        _lengths = await _harness.Query<int>($"SELECT CAST(max_length AS int) AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'ClosedStreams') AND name IN ('StreamType', 'StreamId', 'EventSourceId', 'EventSourceType')");
        _rows = await Rows();
        _primaryKey = await _harness.Query<string>($"SELECT c.name AS [Value] FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id WHERE i.object_id = OBJECT_ID(N'ClosedStreams') AND i.is_primary_key = 1 ORDER BY ic.key_ordinal");
        await _harness.Apply(true);
        _rowsAfterSecondRun = await Rows();
    }

    Task<List<string>> Rows() => _harness.Query<string>($"SELECT CONCAT(CONVERT(nvarchar(max), CAST([StreamId] AS varbinary(max)), 2), '/', CONVERT(nvarchar(max), CAST([EventSourceId] AS varbinary(max)), 2), '/', [SequenceNumber], '/', CONVERT(varchar(19), [ClosedAt], 126)) AS [Value] FROM [ClosedStreams] ORDER BY [SequenceNumber]");

    static string Hex(string value) => Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(value));

    [Fact] void should_compare_every_key_column_exactly() => _collations.ShouldEqual(Enumerable.Repeat("Latin1_General_100_BIN2", 4).ToList());
    [Fact] void should_widen_every_key_column_to_256_characters() => _lengths.ShouldEqual(Enumerable.Repeat(512, 4).ToList());

    [Fact]
    void should_keep_every_row_and_terminate_trailing_spaces() =>
        _rows.ShouldEqual([
            $"{Hex("Alice")}/{Hex("Alice")}/1/2024-01-02T03:04:05",
            $"{Hex("bob \u0001")}/{Hex("bob \u0001")}/2/",
            $"{Hex("carol\u0001\u0001")}/{Hex("carol\u0001\u0001")}/3/"]);

    [Fact] void should_create_the_primary_key_again() => _primaryKey.ShouldEqual(["EventSequenceId", "StreamType", "StreamId", "EventSourceId", "EventSourceType", "Owner", "Dimensions"]);
    [Fact] void should_change_nothing_when_run_again() => _rowsAfterSecondRun.ShouldEqual(_rows);
}
