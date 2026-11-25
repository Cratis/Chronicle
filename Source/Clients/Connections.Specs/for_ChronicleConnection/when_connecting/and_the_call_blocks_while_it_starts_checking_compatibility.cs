// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_call_blocks_while_it_starts_checking_compatibility : given.a_connection_with_several_endpoints
{
    protected override bool SkipCompatibilityCheck => false;

    readonly ManualResetEventSlim _release = new();
    readonly ManualResetEventSlim _blocked = new();

    async Task Because()
    {
        // What a token provider that never answers does: the call blocks the thread that starts it, before any task exists to wait on.
        _compatibility[_a] = () =>
        {
            _blocked.Set();
            _release.Wait(TimeSpan.FromSeconds(30));
            return Task.FromResult(new CompatibilityResponse { IsCompatible = true });
        };
        _compatibility[_b] = () => Task.FromResult(new CompatibilityResponse { IsCompatible = true });
        _streams[_b] = Healthy;

        var connecting = StartConnecting();
        _blocked.Wait(TimeSpan.FromSeconds(10));
        await _time.WhenNextTimer();
        _time.Advance(TimeSpan.FromSeconds(5));
        await connecting.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    void Destroy() => _release.Set();

    [Fact] void should_release_the_connect_lock_and_move_on_to_the_next_endpoint() => _connection.CurrentServerAddress.ShouldEqual(_b);
    [Fact] void should_be_connected() => _isConnected.ShouldBeTrue();
    [Fact] void should_report_the_endpoint_that_did_not_answer() => _logger.Messages.ShouldContain(_ => _.Contains("node-a:35000") && _.Contains("the compatibility check timed out"));
}
