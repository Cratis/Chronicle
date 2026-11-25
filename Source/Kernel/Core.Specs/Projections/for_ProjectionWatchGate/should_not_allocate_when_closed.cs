// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGate;

public class should_not_allocate_when_closed : Specification
{
    ProjectionWatchGate _gate;
    long _allocated;

    void Establish()
    {
        _gate = new(new SettableTimeProvider());
        _ = _gate.IsOpen;
    }

    void Because()
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = _gate.IsOpen;
        }

        _allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact] void should_not_allocate() => _allocated.ShouldEqual(0L);
}
