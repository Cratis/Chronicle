// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_a_clean_second_route_joins_during_a_held_refresh : given.a_cached_database
{
    SinkProvenanceState _state;
    Task _pending;
    Exception? _failure;

    async Task Establish()
    {
        _state = await _cached.ReadState();
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(2));
        _pending = _lease.Ensure();
    }

    async Task Because()
    {
        var second = Substitute.For<ISinkProvenanceStorage>();
        second.ReadState().Returns(_ => _state);
        try
        {
            await _cache.Bind("second-route", () => second).GetLease();
        }
        finally
        {
            _raw.Release();
        }
        _failure = await Catch.Exception(() => _pending.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_read_again_including_the_joined_route() => _failure.ShouldBeNull();
    [Fact] void should_admit_on_the_observation_covering_both_routes() => Catch.Exception(_lease.Check).ShouldBeNull();
}
