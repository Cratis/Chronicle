// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueueRouter.when_resolving_the_queue_index;

/// <summary>
/// The queue an observer is assigned to must be derived the same way in every process and on every run, or two silos
/// - or the same silo after a reactivation - disagree on where an observer lives. The assignment is pinned to golden
/// values here: a per-process randomized hash such as <see cref="string.GetHashCode()"/> cannot reproduce them.
/// </summary>
public class for_known_observer_identifiers : given.a_router
{
    int _a;
    int _b;
    int _c;
    int _d;

    void Because()
    {
        _a = _router.GetQueueIndexFor(ObserverKeyFor("observer-a"));
        _b = _router.GetQueueIndexFor(ObserverKeyFor("observer-b"));
        _c = _router.GetQueueIndexFor(ObserverKeyFor("observer-c"));
        _d = _router.GetQueueIndexFor(ObserverKeyFor("observer-d"));
    }

    [Fact] void should_assign_the_first_observer_to_its_known_queue() => _a.ShouldEqual(1);
    [Fact] void should_assign_the_second_observer_to_its_known_queue() => _b.ShouldEqual(0);
    [Fact] void should_assign_the_third_observer_to_its_known_queue() => _c.ShouldEqual(3);
    [Fact] void should_assign_the_fourth_observer_to_its_known_queue() => _d.ShouldEqual(2);
}
