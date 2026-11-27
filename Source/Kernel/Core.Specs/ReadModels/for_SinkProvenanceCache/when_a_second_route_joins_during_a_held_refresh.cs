// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_a_second_route_joins_during_a_held_refresh : given.a_cached_database
{
    SinkProvenanceState _secondState;
    ISinkProvenanceStorage _second;
    Task _pending;
    Exception? _failure;

    async Task Establish()
    {
        _secondState = await _cached.ReadState();
        _second = Substitute.For<ISinkProvenanceStorage>();
        _second.ReadState().Returns(_ => _secondState);
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(2));
        _pending = _lease.Ensure();
    }

    async Task Because()
    {
        try
        {
            await _cache.Bind("second-route", () => _second).GetLease();
            _secondState = _secondState with { Restricted = true };
        }
        finally
        {
            _raw.Release();
        }
        _failure = await Catch.Exception(() => _pending.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
    }

    [Fact] void should_not_admit_on_the_observation_that_omitted_the_join() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_allow_delivery_after_the_observation_that_omitted_the_join() => Catch.Exception(_lease.Check).ShouldBeOfExactType<InvalidOperationException>();
}
