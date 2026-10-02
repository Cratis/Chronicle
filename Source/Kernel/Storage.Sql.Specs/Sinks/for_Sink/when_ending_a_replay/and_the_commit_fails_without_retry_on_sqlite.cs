// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_the_commit_fails_without_retry_on_sqlite : given.a_replay_with_a_commit_failure<SqlSinkHarness>
{
    SqlSinkHarness _sqlHarness;
    int _markerCount;

    protected override bool FailAfterCommit => false;

    protected override SqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => _sqlHarness = new() { Interceptors = [interceptor] };

    async Task Because()
    {
        await PromoteReplay();
        await using var connection = new SqliteConnection(_sqlHarness.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM chronicle_replay_promotions";
        _markerCount = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    [Fact] void should_report_the_commit_failure() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_restore_the_original_primary() => _primaryCount.ShouldEqual(1);
    [Fact] void should_roll_back_the_backup_rename() => _revertCount.ShouldBeNull();
    [Fact] void should_roll_back_the_completion_marker_with_the_renames() => _markerCount.ShouldEqual(0);
}
