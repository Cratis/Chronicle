// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

/// <summary>
/// A dormant observer has no activation to report from: its gauge entry is released with the activation, so nothing is left behind.
/// </summary>
public class when_the_observer_is_released_while_reporting_gauges : given.a_subscribed_client_observer
{
    List<ObserverGaugeEntry> _registered;
    List<ObserverGaugeEntry> _released;

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

    async Task Establish()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    async Task Because() => await Deactivate();

    [Fact] void should_have_reported_while_resident() => _registered.Count.ShouldBeGreaterThan(0);
    [Fact] void should_release_every_gauge_entry() => _released.Count.ShouldEqual(_registered.Count);
}
