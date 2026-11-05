// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionSubscriptionThrottle;

/// <summary>
/// Eight grants exclude a ninth until a holder releases admission.
/// </summary>
public class when_capacity_is_exhausted : Specification
{
    readonly ProjectionSubscriptionThrottle _throttle = new();
    bool _allGranted = true;
    bool _ninthWasQueued;
    bool _ninthGranted;

    async Task Because()
    {
        for (var index = 0; index < 8; index++) _allGranted &= await _throttle.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        var ninth = _throttle.WaitAsync(Timeout.InfiniteTimeSpan, CancellationToken.None);
        _ninthWasQueued = !ninth.IsCompleted;
        _throttle.Release();
        _ninthGranted = await ninth.WaitAsync(TimeSpan.FromSeconds(30));
        for (var index = 0; index < 8; index++) _throttle.Release();
    }

    [Fact] void should_grant_eight() => _allGranted.ShouldBeTrue();
    [Fact] void should_queue_the_ninth() => _ninthWasQueued.ShouldBeTrue();
    [Fact] void should_grant_after_release() => _ninthGranted.ShouldBeTrue();
}
