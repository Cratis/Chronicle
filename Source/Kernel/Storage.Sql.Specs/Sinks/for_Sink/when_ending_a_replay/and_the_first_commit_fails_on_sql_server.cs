// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(SqlServerCollection.Name)]
public class and_the_first_commit_fails_on_sql_server(SqlServerFixture fixture) : given.a_replay_with_a_commit_failure<SqlServerSinkHarness>
{
    protected override bool FailAfterCommit => false;

    protected override SqlServerSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    Task Because() => PromoteReplay();

    [Fact] void should_complete_the_promotion() => _error.ShouldBeNull();
    [Fact] void should_retry_the_entire_transaction() => CommitAttempts.ShouldEqual(2);
    [Fact] void should_publish_the_rebuilt_state() => _primaryCount.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _revertCount.ShouldEqual(1);
}
