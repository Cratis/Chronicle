// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(SqlServerCollection.Name)]
public class and_the_first_commit_fails_on_sql_server(SqlServerFixture fixture) : given.a_replay_with_a_commit_failure<SqlServerSinkHarness>
{
    int _primaryBeforeRetry;
    int? _replayBeforeRetry;

    protected override bool FailAfterCommit => false;

    protected override SqlServerSinkHarness CreateHarnessWithInterceptor(IInterceptor interceptor) => new() { Fixture = fixture, Interceptors = [interceptor] };

    protected override async Task BeforeRetry()
    {
        // Inspect the rolled-back state before another attempt can repair or obscure it.
        var primary = await _sink.GetInstances();
        _primaryBeforeRetry = Convert.ToInt32(((IDictionary<string, object?>)primary.Instances.Single())["count"], CultureInfo.InvariantCulture);
        _replayBeforeRetry = await CurrentCountOrNull();
    }

    Task Because() => PromoteReplay();

    [Fact] void should_roll_back_the_primary_rename_before_retrying() => _primaryBeforeRetry.ShouldEqual(1);
    [Fact] void should_roll_back_the_replay_rename_before_retrying() => _replayBeforeRetry.ShouldEqual(2);
    [Fact] void should_roll_back_the_completion_marker_before_retrying() => MarkerCountBeforeRetry.ShouldEqual(0);
    [Fact] void should_complete_the_promotion() => _error.ShouldBeNull();
    [Fact] void should_retry_the_entire_transaction() => CommitAttempts.ShouldEqual(2);
    [Fact] void should_publish_the_rebuilt_state() => _primaryCount.ShouldEqual(2);
    [Fact] void should_preserve_the_original_backup() => _revertCount.ShouldEqual(1);
}
