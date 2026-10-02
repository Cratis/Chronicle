// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(SqlServerCollection.Name)]
public class and_the_replay_table_becomes_empty_before_retry_on_sql_server(SqlServerFixture fixture) : given.a_replay_with_a_commit_failure<SqlServerSinkHarness>
{
    bool _replayEmptied;

    protected override bool FailAfterCommit => false;

    protected override SqlServerSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task BeforeRetry()
    {
        // The first attempt rolled back. Remove the replay rows before the retried body inspects them.
        await _sink.PrepareInitialRun();
        _replayEmptied = true;
    }

    Task Because() => PromoteReplay();

    [Fact] void should_recheck_the_replay_table_inside_the_retry() => _replayEmptied.ShouldBeTrue();
    [Fact] void should_report_the_unexpectedly_empty_replay() => _error.ShouldBeOfExactType<ReplayTableBecameEmpty>();
    [Fact] void should_not_commit_another_swap() => CommitAttempts.ShouldEqual(1);
    [Fact] void should_leave_the_original_primary_untouched() => _primaryCount.ShouldEqual(1);
    [Fact] void should_not_publish_a_backup() => _revertCount.ShouldBeNull();
}
