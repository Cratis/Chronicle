// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ClosedStreams.for_ClosedStreamsConstraintStorage.when_closing_streams_that_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class on_sql_server(SqlServerFixture fixture) : conformance(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    List<string> _collations;
    List<string> _stored;
    ClosedStream[] _ordered;

    async Task Because()
    {
        _collations = await _harness.Query<string>($"SELECT collation_name AS [Value] FROM sys.columns WHERE object_id = OBJECT_ID(N'ClosedStreams') AND name IN ('StreamType', 'StreamId', 'EventSourceId', 'EventSourceType')");
        _stored = await _harness.Query<string>($"SELECT CONVERT(nvarchar(max), CAST([StreamId] AS varbinary(max)), 2) AS [Value] FROM [ClosedStreams]");
        _ordered = [.. await Storage.GetAll()];
    }

    static string Hex(string value) => Convert.ToHexString(System.Text.Encoding.Unicode.GetBytes(value));

    [Fact] void should_compare_every_key_column_exactly() => _collations.ShouldEqual(Enumerable.Repeat("Latin1_General_100_BIN2", 4).ToList());
    [Fact] void should_store_a_trailing_space_with_a_terminator() => _stored.ShouldContain(Hex("Order-1 \u007F"));
    [Fact] void should_store_the_terminator_with_another_terminator() => _stored.ShouldContain(Hex("Order-1\u007F\u007F"));
    [Fact] void should_list_closures_in_code_point_order() => _ordered.Select(_ => _.Scope.EventSourceId!.Value).ShouldEqual(_ordered.Select(_ => _.Scope.EventSourceId!.Value).Order(StringComparer.Ordinal));
}
