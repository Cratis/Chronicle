// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionSubscriptionThrottle;

/// <summary>
/// Cancellation and timeout confer no grant and do not change capacity.
/// </summary>
public class when_a_waiter_is_cancelled : Specification
{
    readonly ProjectionSubscriptionThrottle _throttle = new();
    readonly CancellationTokenSource _cancellation = new();
    Exception? _error;
    bool _timedOut;
    bool _capacityIntact = true;

    async Task Because()
    {
        for (var index = 0; index < 8; index++) await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _timedOut = !await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        var queued = _throttle.WaitAsync(Timeout.InfiniteTimeSpan, _cancellation.Token);
        await _cancellation.CancelAsync();
        _error = await Catch.Exception(() => queued);
        for (var index = 0; index < 8; index++) _throttle.Release();
        for (var index = 0; index < 8; index++) _capacityIntact &= await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        _capacityIntact &= !await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        for (var index = 0; index < 8; index++) _throttle.Release();
    }

    void Destroy() => _cancellation.Dispose();

    [Fact] void should_report_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_report_no_grant_on_timeout() => _timedOut.ShouldBeTrue();
    [Fact] void should_preserve_exact_capacity() => _capacityIntact.ShouldBeTrue();
}
