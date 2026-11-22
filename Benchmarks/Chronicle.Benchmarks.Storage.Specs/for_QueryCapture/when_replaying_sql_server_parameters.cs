// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data;
using Cratis.Specifications;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_QueryCapture;

public class when_replaying_sql_server_parameters : Specification
{
    SqlParameter _replayed = default!;
    string _table = default!;

    void Because()
    {
        using var captured = new SqlCommand("SELECT * FROM [dbo].[events] WHERE [sequence_number] >= @from");
        var parameter = captured.Parameters.Add("@from", SqlDbType.Decimal);
        parameter.Precision = 20;
        parameter.Scale = 0;
        parameter.Value = 18446744073709551615m;
        parameter.Size = 20;
        var capture = new QueryCapture();
        using (capture.Begin("cursor")) capture.Record(captured);
        var query = capture.Queries["cursor"];
        parameter.Value = 0m;
        using var command = new SqlCommand();
        SqlPlanQueries.AddParameter(command, query.Parameters.Single());
        _replayed = command.Parameters[0];
        _table = SqlPlanQueries.TableOf(query.Sql);
    }

    [Fact] void should_preserve_sql_db_type() => _replayed.SqlDbType.ShouldEqual(SqlDbType.Decimal);
    [Fact] void should_preserve_precision() => _replayed.Precision.ShouldEqual((byte)20);
    [Fact] void should_preserve_scale() => _replayed.Scale.ShouldEqual((byte)0);
    [Fact] void should_preserve_size() => _replayed.Size.ShouldEqual(20);
    [Fact] void should_snapshot_the_unsigned_sequence_value() => _replayed.Value.ShouldEqual(18446744073709551615m);
    [Fact] void should_resolve_a_bracketed_schema_qualified_fixture() => _table.ShouldEqual("events");
}
