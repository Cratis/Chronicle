// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_a_refresh_stalls_beyond_the_clean_window : given.a_cached_database
{
    Exception _expired;
    Exception _late;
    bool _held;

    void Because()
    {
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(1));
        _held = _raw.HasHeldRead;
        _clock.Advance(TimeSpan.FromSeconds(2));
        _expired = Catch.Exception(_lease.Check);
        _raw.Release();
        _late = Catch.Exception(_lease.Check);
    }

    [Fact] void should_keep_one_refresh_in_flight() => _raw.Reads.ShouldEqual(1);
    [Fact] void should_have_started_the_controlled_refresh() => _held.ShouldBeTrue();
    [Fact] void should_not_authorize_on_an_expired_snapshot() => _expired.ShouldNotBeNull();
    [Fact] void should_not_restart_the_window_when_a_late_clean_read_returns() => _late.ShouldNotBeNull();
    [Fact] void should_not_treat_expiry_as_a_mark() => _lease.Revoked.IsCancellationRequested.ShouldBeFalse();
}
