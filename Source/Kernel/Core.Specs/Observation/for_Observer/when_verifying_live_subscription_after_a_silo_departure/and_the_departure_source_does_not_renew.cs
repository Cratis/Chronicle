// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_the_departure_source_does_not_renew : given.an_observing_observer
{
    public and_the_departure_source_does_not_renew()
    {
        var departures = Substitute.For<ISiloDepartures>();
        departures.Next.Returns(Task.CompletedTask);
        _silo.AddService(departures);
    }

    [Fact] void should_not_schedule_checks_of_the_live_subscription() => _observer.HasDepartureChecksScheduled.ShouldBeFalse();
}
