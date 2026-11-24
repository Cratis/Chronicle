// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Chronicle.Concepts.Events.for_EventTypeSet;

public class when_the_last_reference_goes_away : Specification
{
    WeakReference _first;
    EventTypeSet _second;

    void Because()
    {
        _first = CreateAndForget();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        _second = EventTypeSet.From([new EventType("collected-type", 1)]);
    }

    [Fact] void should_not_keep_the_first_alive() => _first.IsAlive.ShouldBeFalse();
    [Fact] void should_create_a_fresh_set_for_the_same_list() => _second.Count.ShouldEqual(1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference CreateAndForget() => new(EventTypeSet.From([new EventType("collected-type", 1)]));
}
