// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_reconnecting;

public class and_the_first_endpoint_is_slow_to_check_compatibility_and_never_sends_a_keep_alive : given.a_connection_with_several_endpoints
{
    protected override bool SkipCompatibilityCheck => false;

    DateTimeOffset _started;

    async Task Because()
    {
        _started = _time.GetUtcNow();
        var slowCheck = new TaskCompletionSource<CompatibilityResponse>();
        _compatibility[_a] = () => slowCheck.Task;
        _streams[_b] = Healthy;
        var connecting = StartConnecting();

        // The compatibility check is waiting on the endpoint cap; let 3 of the 5 seconds pass, then finish the check.
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(3));
        slowCheck.SetResult(new CompatibilityResponse { IsCompatible = true });

        // The keep-alive stream is stalled, so only the 2 seconds that remain of the cap may be spent on it.
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(2));
        await connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_fail_over_to_the_second_endpoint() => TriedInOrder.ShouldEqual("node-a,node-b");
    [Fact] void should_spend_no_more_than_one_endpoint_cap() => (_time.GetUtcNow() - _started).ShouldEqual(TimeSpan.FromSeconds(5));
}
