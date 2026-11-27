// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_reusing_a_warm_physical_database : given.a_cached_database
{
    async Task Because()
    {
        for (var i = 0; i < 100; i++)
        {
            await _cached.ReadState();
            await _cached.IsQuarantined();
            await (await _cached.GetLease()).Ensure();
        }
    }

    [Fact] void should_do_no_marker_io_per_operation() => _raw.Reads.ShouldEqual(0);
}
