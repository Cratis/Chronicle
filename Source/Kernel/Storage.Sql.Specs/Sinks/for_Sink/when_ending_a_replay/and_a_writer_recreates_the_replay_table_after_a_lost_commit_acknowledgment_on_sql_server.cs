// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(SqlServerCollection.Name)]
public class and_a_writer_recreates_the_replay_table_after_a_lost_commit_acknowledgment_on_sql_server(SqlServerFixture fixture) : given.a_replay_with_a_commit_failure<SqlServerSinkHarness>
{
    int? _lateWriteCount;

    protected override bool FailAfterCommit => true;

    protected override SqlServerSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task AfterCommit()
    {
        // Replay routing is still active: a late writer recreates and populates the replay table.
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        _lateWriteCount = await CurrentCountOrNull();
    }

    Task Because() => PromoteReplay();

    [Fact] void should_interleave_the_write_before_verification() => _lateWriteCount.ShouldEqual(3);
    [Fact] void should_recognize_the_successful_commit() => _error.ShouldBeNull();
    [Fact] void should_not_repeat_the_swap() => CommitAttempts.ShouldEqual(1);
    [Fact] void should_publish_the_rebuilt_state() => _primaryCount.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _revertCount.ShouldEqual(1);
}
