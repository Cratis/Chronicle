// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_the_replay_table_becomes_empty_before_retry_on_postgresql(PostgreSqlFixture fixture) : given.a_replay_with_a_commit_failure<PostgreSqlSinkHarness>
{
    bool _replayEmptied;

    protected override bool FailAfterCommit => true;

    protected override PostgreSqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task AfterCommit()
    {
        // A late write recreates a nonempty replay table, making verification inconclusive.
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
    }

    protected override async Task BeforeRetry()
    {
        // Remove those rows after verification but before the retried body inspects the replay table.
        await _sink.PrepareInitialRun();
        _replayEmptied = true;
    }

    Task Because() => PromoteReplay();

    [Fact] void should_recheck_the_replay_table_inside_the_retry() => _replayEmptied.ShouldBeTrue();
    [Fact] void should_report_the_unexpectedly_empty_replay() => _error.ShouldBeOfExactType<ReplayTableBecameEmpty>();
    [Fact] void should_not_commit_another_swap() => CommitAttempts.ShouldEqual(1);
    [Fact] void should_leave_the_rebuilt_primary_untouched() => _primaryCount.ShouldEqual(2);
    [Fact] void should_leave_the_original_backup_untouched() => _revertCount.ShouldEqual(1);
}
