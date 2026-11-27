// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_reading_lag.given;

public abstract class an_observer_publishing_a_stalled_sample : an_observer_with_a_stalled_sample
{
    List<ObserverGaugeEntry> _registered;

    protected ObserverLagSample Published => _registered[^1].Snapshot.LagSample!;

    protected override IObserverGauges? CreateObserverGauges()
    {
        _registered = [];
        var gauges = Substitute.For<IObserverGauges>();
        gauges.Register(Arg.Any<ObserverKey>(), Arg.Any<ObserverType>()).Returns(call =>
        {
            var entry = new ObserverGaugeEntry(call.Arg<ObserverKey>(), call.Arg<ObserverType>(), _ => { });
            _registered.Add(entry);
            return entry;
        });
        return gauges;
    }
}
