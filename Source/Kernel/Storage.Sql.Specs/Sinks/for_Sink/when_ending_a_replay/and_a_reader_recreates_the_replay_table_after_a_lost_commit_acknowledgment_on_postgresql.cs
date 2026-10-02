// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_a_reader_recreates_the_replay_table_after_a_lost_commit_acknowledgment_on_postgresql(PostgreSqlFixture fixture) : given.a_replay_with_a_commit_failure<PostgreSqlSinkHarness>
{
    ExpandoObject? _concurrentRead;
    bool _readerCompleted;

    protected override bool FailAfterCommit => true;

    protected override PostgreSqlSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task AfterCommit()
    {
        // Replay routing is still active: this fresh scope recreates the renamed-away replay table.
        _concurrentRead = await _sink.FindOrDefault(_key);
        _readerCompleted = true;
    }

    Task Because() => PromoteReplay();

    [Fact] void should_interleave_the_reader_before_verification() => _readerCompleted.ShouldBeTrue();
    [Fact] void should_read_the_recreated_empty_replay_table() => _concurrentRead.ShouldBeNull();
    [Fact] void should_recognize_the_successful_commit() => _error.ShouldBeNull();
    [Fact] void should_not_repeat_the_swap() => CommitAttempts.ShouldEqual(1);
    [Fact] void should_publish_the_rebuilt_state() => _primaryCount.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _revertCount.ShouldEqual(1);
}
