// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_the_commit_acknowledgment_is_lost_on_postgresql(PostgreSqlFixture fixture) : given.a_replay_with_a_commit_failure<PostgreSqlSinkHarness>
{
    protected override bool FailAfterCommit => true;

    protected override PostgreSqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    Task Because() => PromoteReplay();

    [Fact] void should_recognize_the_successful_commit() => _error.ShouldBeNull();
    [Fact] void should_not_repeat_the_swap() => CommitAttempts.ShouldEqual(1);
    [Fact] void should_publish_the_rebuilt_state() => _primaryCount.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _revertCount.ShouldEqual(1);
}
