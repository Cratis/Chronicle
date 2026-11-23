// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_ownership_moves_to_another_event_sequence : given.a_live_observer_with_guarded_state
{
    List<ObserverGaugeEntry> _registered;
    List<ObserverGaugeEntry> _released;
    int _registeredBeforeMoving;

    void Establish() => _registeredBeforeMoving = _registered.Count;

    async Task Because()
    {
        await MoveDefinition();
        await _observer.SetHandledStats(99);
        await _observer.SetHandledStats(100);
        await _observer.PartitionFailed("late", 101UL, ["Late failure"], "Stack");
    }

    protected override IObserverGauges? CreateObserverGauges()
    {
        _registered = [];
        _released = [];
        var gauges = Substitute.For<IObserverGauges>();
        gauges.Register(Arg.Any<ObserverKey>(), Arg.Any<ObserverType>()).Returns(call =>
        {
            var entry = new ObserverGaugeEntry(call.Arg<ObserverKey>(), call.Arg<ObserverType>(), _released.Add);
            _registered.Add(entry);
            return entry;
        });
        return gauges;
    }

    [Fact] void should_have_reported_while_it_owned_the_definition() => _registeredBeforeMoving.ShouldBeGreaterThan(0);
    [Fact] void should_not_register_again_after_losing_ownership() => _registered.Count.ShouldEqual(_registeredBeforeMoving);
    [Fact] void should_release_what_it_registered() => _released.ShouldContainOnly(_registered);
}
