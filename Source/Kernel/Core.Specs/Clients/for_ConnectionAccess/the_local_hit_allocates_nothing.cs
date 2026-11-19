// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

/// <summary>
/// Measures several batches and keeps the smallest. A per-call allocation shows up in every batch, while one-off
/// runtime or profiler work on the thread (tier-up, coverage instrumentation) lands in at most a few of them.
/// </summary>
public class the_local_hit_allocates_nothing : given.a_connection_check
{
    const int Batches = 10;
    const int CallsPerBatch = 1_000;

    long _allocated;

    void Establish()
    {
        _ownership.Claim(Id, Alice);
        for (var index = 0; index < 10_000; index++) Check();
    }
    void Because()
    {
        _allocated = long.MaxValue;
        for (var batch = 0; batch < Batches && _allocated > 0; batch++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < CallsPerBatch; index++) Check();
            _allocated = Math.Min(_allocated, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }

    ConnectionAccessOutcome Check()
    {
        var verification = _ownership.Verify(_factory, Id, Alice, Timeout);
        if (!verification.IsCompletedSuccessfully) throw new InvalidOperationException("The local check did not complete synchronously.");
        return verification.Result;
    }

    [Fact] void should_allocate_no_bytes_in_a_steady_state_batch() => _allocated.ShouldEqual(0L);
}
