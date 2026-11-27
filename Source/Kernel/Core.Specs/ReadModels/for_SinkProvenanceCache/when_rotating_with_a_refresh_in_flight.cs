// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEVELOPMENT
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_rotating_with_a_refresh_in_flight : Specification
{
    DevelopmentSinkProvenanceCache _cache;
    Exception? _oldError;
    Exception? _newError;
    bool _waitedForRefresh;

    async Task Because()
    {
        var clock = new given.ManualClock();
        var raw = new given.ControlledProvenance();
        await raw.CompleteInventory(false);
        _cache = new(clock);
        var old = await _cache.Bind("route", () => raw).GetLease();
        raw.Hold = true;
        clock.Advance(TimeSpan.FromSeconds(1));
        await raw.MarkQuarantined();
        var draining = _cache.RotateAfterWipe();
        _waitedForRefresh = raw.HasHeldRead && !draining.IsCompleted;
        _oldError = Catch.Exception(old.Check);
        raw.Release();
        await draining;
        var replacement = await _cache.Bind("route", () => raw).GetLease();
        _newError = await Catch.Exception(replacement.Ensure);
    }

    async Task Destroy() => await _cache.DisposeAsync();

    [Fact] void should_wait_for_the_old_refresh() => _waitedForRefresh.ShouldBeTrue();
    [Fact] void should_revoke_the_old_lease_before_the_refresh_finishes() => _oldError.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_a_fresh_lease_if_the_restriction_survived() => _newError.ShouldBeOfExactType<InvalidOperationException>();
}
#endif
