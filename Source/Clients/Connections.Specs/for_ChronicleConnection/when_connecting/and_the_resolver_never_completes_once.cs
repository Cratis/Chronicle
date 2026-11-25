// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_resolver_never_completes_once : given.a_connection_with_several_endpoints
{
    int _resolutions;
    TaskCompletionSource _firstResolutionStarted;

    async Task Because()
    {
        _firstResolutionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _streams[_a] = Healthy;
        _resolver.Resolve(Arg.Any<ChronicleConnectionString>())
            .Returns(_ =>
            {
                if (Interlocked.Increment(ref _resolutions) == 1)
                {
                    _firstResolutionStarted.SetResult();
                    return new TaskCompletionSource<IReadOnlyList<ChronicleServerAddress>>().Task;
                }

                return Task.FromResult<IReadOnlyList<ChronicleServerAddress>>([_a]);
            });

        var stuck = StartConnecting();
        await _firstResolutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        var giveUpAt = DateTimeOffset.UtcNow.AddSeconds(10);
        while (!stuck.IsCompleted)
        {
            giveUpAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
            _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds));
            await Task.Delay(5);
        }

        await stuck;
        await StartConnecting().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_release_the_connect_lock_for_the_next_attempt() => _isConnected.ShouldBeTrue();
    [Fact] void should_connect_to_the_resolved_server() => _connection.CurrentServerAddress.ShouldEqual(_a);
    [Fact] void should_resolve_again_for_the_next_attempt() => _resolutions.ShouldEqual(2);
    [Fact] void should_report_that_the_servers_could_not_be_resolved() => _logger.Messages.ShouldContain(_ => _.StartsWith("Could not resolve the Chronicle servers"));
}
