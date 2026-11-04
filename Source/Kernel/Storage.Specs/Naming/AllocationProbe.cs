// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming;

/// <summary>
/// Measures the bytes allocated on the calling thread by repeating an operation.
/// </summary>
static class AllocationProbe
{
    const int Warmup = 200;

    public static long Measure(int iterations, Action operation)
    {
        for (var i = 0; i < Warmup; i++)
        {
            operation();
        }

        GC.Collect();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < iterations; i++)
        {
            operation();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
