// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_many_callers_acquire_the_same_cold_route : given.a_cached_database
{
    int _reads;
    SinkProvenanceLease[] _leases;

    async Task Because()
    {
        await using var cache = new SinkProvenanceCache(_clock);
        _raw.Hold = true;
        var handle = cache.Bind("cold", () => _raw);
        var callers = Enumerable.Range(0, 100).Select(_ => handle.GetLease()).ToArray();
        _reads = _raw.Reads;
        _raw.Release();
        _leases = await Task.WhenAll(callers);
    }

    [Fact] void should_single_flight_the_metadata_lookup() => _reads.ShouldEqual(1);
    [Fact] void should_return_one_shared_physical_lease() => _leases.All(_ => ReferenceEquals(_, _leases[0])).ShouldBeTrue();
}
