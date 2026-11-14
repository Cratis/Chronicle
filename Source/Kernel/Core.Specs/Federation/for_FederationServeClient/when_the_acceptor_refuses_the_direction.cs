// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_the_acceptor_refuses_the_direction : given.a_serve_client
{
    async Task Because()
    {
        _serve.Script.Add(new ServePullRequest { Status = FederatedEventBatchStatus.Denied });
        Start();
        await _serve.WhenCalls(1);
        await _clock.WaitForPendingTimer();
        _clock.Advance(Timing.DeniedBackoff);
        await _serve.WhenCalls(2);
    }

    [Fact] void should_try_again_on_the_same_connection() => _serve.Calls.ShouldBeGreaterThanOrEqual(2);
    [Fact] void should_count_the_refusals() => _client.Refusals.ShouldBeGreaterThanOrEqual(1);
    [Fact] void should_not_be_serving() => _client.IsServing.ShouldBeFalse();
    [Fact] void should_read_nothing() => _reads.ShouldBeEmpty();
    [Fact] void should_leave_the_connection_open() => _connections.Closed.ShouldEqual(0);
}
