// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_disposing_with_an_old_refresh_in_flight : given.a_cached_database
{
    Exception _error;
    int _reads;

    void Because()
    {
        _raw.Hold = true;
        _clock.Advance(TimeSpan.FromSeconds(1));
        _cache.Dispose();
        _raw.Release();
        _clock.Advance(TimeSpan.FromSeconds(10));
        _reads = _raw.Reads;
        _error = Catch.Exception(_lease.Check);
    }

    [Fact] void should_not_publish_old_worker_results_after_disposal() => _error.ShouldNotBeNull();
    [Fact] void should_stop_scheduling_refreshes() => _reads.ShouldEqual(1);
}
