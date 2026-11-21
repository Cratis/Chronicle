// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Benchmarks.Storage.Hosts;
using Cratis.Specifications;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Cratis.Chronicle.Benchmarks.Storage.for_QueryCapture;

public class when_capturing_a_labelled_query : Specification
{
    QueryCapture _capture = default!;
    void Because()
    {
        _capture = new();
        var interceptor = new CountingCommandInterceptor(new(), _capture);
        using var command = new SqliteCommand("SELECT * FROM events WHERE id=@id");
        var parameter = command.Parameters.AddWithValue("@id", 42);
        interceptor.ReaderExecuting(command, null!, default);
        using (_capture.Begin("cursor-page"))
        {
            command.CommandText = "PRAGMA table_info(events)";
            interceptor.ReaderExecuting(command, null!, default);
            command.CommandText = "SELECT * FROM events WHERE id=@id";
            interceptor.ReaderExecuting(command, null!, default);
            parameter.Value = 100;
            command.CommandText = "SELECT ignored";
            interceptor.ReaderExecuting(command, null!, default);
        }
    }
    [Fact] void should_only_capture_the_first_labelled_select() => _capture.Queries.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_sql() => _capture.Queries["cursor-page"].Sql.ShouldContain("WHERE id=@id");
    [Fact] void should_snapshot_parameters() => _capture.Queries["cursor-page"].Parameters[0].Value.ShouldEqual(42);
}
