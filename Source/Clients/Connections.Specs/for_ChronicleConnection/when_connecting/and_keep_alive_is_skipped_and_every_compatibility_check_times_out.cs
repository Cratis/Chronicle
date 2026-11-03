// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_keep_alive_is_skipped_and_every_compatibility_check_times_out : given.a_connection_with_several_endpoints
{
    Exception? _result;

    protected override bool SkipCompatibilityCheck => false;
    protected override bool SkipKeepAlive => true;

    async Task Because()
    {
        _compatibility[_a] = () => new TaskCompletionSource<CompatibilityResponse>().Task;
        _compatibility[_b] = () => new TaskCompletionSource<CompatibilityResponse>().Task;
        var connecting = StartConnecting();

        // The first endpoint is held to its cap; the last one gets what remains of the connect budget.
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds - 5));
        try
        {
            await connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        }
        catch (Exception ex)
        {
            _result = ex;
        }
    }

    [Fact] void should_fail_the_caller_with_a_connection_timeout() => _result.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_not_be_connected() => _isConnected.ShouldBeFalse();
    [Fact] void should_not_have_connected_to_any_endpoint() => _servedBy.ShouldBeEmpty();
}
