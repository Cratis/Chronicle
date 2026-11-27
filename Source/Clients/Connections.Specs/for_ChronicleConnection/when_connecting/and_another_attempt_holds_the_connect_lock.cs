// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_another_attempt_holds_the_connect_lock : given.a_connection_with_several_endpoints
{
    protected override int ConnectTimeout => 1;
    protected override IReadOnlyList<ChronicleServerAddress> Servers => [_a];

    Exception _error;

    async Task Because()
    {
        // The first attempt never completes its handshake, and the manual clock never moves, so it keeps the lock.
        _ = StartConnecting();
        await WaitUntil(() => _connectsPerServer.GetValueOrDefault(_a) == 1);
        _error = await Catch.Exception(() => _connection.Connect().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System));
    }

    [Fact] void should_time_out_the_waiting_caller() => _error.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_name_the_endpoint_in_the_exception() => _error.Message.ShouldContain("node-a:35000");
    [Fact] void should_log_that_the_lock_wait_timed_out() => _logger.Messages.ShouldContain(_ => _.StartsWith("Gave up waiting 1 seconds for the connect attempt already in progress"));
    [Fact] void should_log_the_endpoint_and_phase_of_the_attempt_in_progress() => _logger.Messages.ShouldContain(_ => _.Contains("endpoint: node-a:35000, phase: awaiting the handshake"));
    [Fact] void should_say_that_no_endpoint_was_attempted_by_the_caller() => _logger.Messages.ShouldContain(_ => _.Contains("no new endpoint was attempted by this caller"));
    [Fact] void should_not_try_any_endpoint_for_the_waiting_caller() => _connectsPerServer[_a].ShouldEqual(1);
}
