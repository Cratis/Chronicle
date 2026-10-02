// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_a_committed_swap_is_reentered_with_a_nonempty_replay_table_on_postgresql(PostgreSqlFixture fixture) : given.a_replay_with_a_commit_failure<PostgreSqlSinkHarness>
{
    protected override bool FailAfterCommit => true;

    protected override PostgreSqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task AfterCommit()
    {
        // Leave a nonempty replay table behind after the swap commits.
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
    }

    async Task Establish() => await _sink.EndReplay(ReplayContext());

    Task Because()
    {
        // A repeated completion enters the transaction body directly, without relying on verification.
        return PromoteReplay();
    }

    [Fact] void should_recognize_the_already_committed_swap() => _error.ShouldBeNull();
    [Fact] void should_leave_the_rebuilt_primary_untouched() => _primaryCount.ShouldEqual(2);
    [Fact] void should_leave_the_original_backup_untouched() => _revertCount.ShouldEqual(1);
}
