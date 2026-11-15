// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_it_is_stopped : given.a_serve_client
{
    int _callsAtStop;
    int _callsLater;
    int _pendingAfterStop;

    async Task Because()
    {
        _serve.Script.Add(new ServePullRequest { Status = FederatedEventBatchStatus.Denied });
        Start();
        await _serve.WhenCalls(1);
        await _clock.WaitForPendingTimer();
        _client.Stop();
        _callsAtStop = _serve.Calls;
        _pendingAfterStop = _clock.PendingTimers;
        _clock.Advance(Timing.DeniedBackoff);
        _callsLater = _serve.Calls;
    }

    [Fact] void should_stop_opening_calls() => _callsLater.ShouldEqual(_callsAtStop);
    [Fact] void should_not_wait_to_open_another_call() => _pendingAfterStop.ShouldEqual(0);
    [Fact] void should_not_be_serving() => _client.IsServing.ShouldBeFalse();
}
