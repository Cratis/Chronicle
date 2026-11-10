// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionAccess;

public class the_local_hit_allocates_nothing : given.a_connection_check
{
    long _allocated;

    void Establish()
    {
        _ownership.Claim(Id, Alice);
        for (var index = 0; index < 10_000; index++) Check();
    }
    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++) Check();
        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    ConnectionAccessOutcome Check()
    {
        var verification = _ownership.Verify(_factory, Id, Alice, Timeout);
        if (!verification.IsCompletedSuccessfully) throw new InvalidOperationException("The local check did not complete synchronously.");
        return verification.Result;
    }

    [Fact] void should_allocate_no_bytes() => _allocated.ShouldEqual(0L);
}
