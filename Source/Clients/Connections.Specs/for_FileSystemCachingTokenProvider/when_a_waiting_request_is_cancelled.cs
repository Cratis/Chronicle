// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_a_waiting_request_is_cancelled : given.a_caching_provider
{
    readonly TaskCompletionSource<string?> _acquisition = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _cancelled;
    string? _result;

    void Establish() => _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns(_acquisition.Task);

    async Task Because()
    {
        var first = _provider.GetAccessToken();
        using var cancellation = new CancellationTokenSource();
        var waiting = _provider.GetAccessToken(cancellation.Token);
        await cancellation.CancelAsync();
        try
        {
            await waiting;
        }
        catch (OperationCanceledException)
        {
            _cancelled = true;
        }

        _acquisition.SetResult("fresh-token");
        await first;
        _result = await _provider.GetAccessToken();
    }

    [Fact] void should_cancel_the_waiter() => _cancelled.ShouldBeTrue();
    [Fact] void should_leave_the_lock_usable() => _result.ShouldEqual("fresh-token");
    [Fact] void should_not_acquire_for_the_cancelled_request() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_leave_no_temporary_files() => Directory.GetFiles(CacheDirectory).Length.ShouldEqual(1);
}
