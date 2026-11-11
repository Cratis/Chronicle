// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_reporting_gauges : an_observer_with_subscription
{
    protected List<ObserverGaugeEntry> _registered;
    protected List<ObserverGaugeEntry> _released;

    protected ObserverGaugeEntry? Entry => _registered.LastOrDefault();

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
}
