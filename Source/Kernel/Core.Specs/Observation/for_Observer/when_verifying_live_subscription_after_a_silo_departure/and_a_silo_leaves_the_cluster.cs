// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services.Observation;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_verifying_live_subscription_after_a_silo_departure;

public class and_a_silo_leaves_the_cluster : given.an_observing_observer
{
    readonly TaskCompletionSource _departure = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _scheduledBeforeDeparture;

    public and_a_silo_leaves_the_cluster()
    {
        var departures = Substitute.For<ISiloDepartures>();
        departures.Next.Returns(_departure.Task, new TaskCompletionSource().Task);
        _silo.AddService(departures);
    }

    void Establish() => _scheduledBeforeDeparture = _observer.HasDepartureChecksScheduled;

    async Task Because()
    {
        _departure.SetResult();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!_observer.HasDepartureChecksScheduled && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), TimeProvider.System);
        }
    }

    [Fact] void should_not_check_before_a_silo_leaves() => _scheduledBeforeDeparture.ShouldBeFalse();
    [Fact] void should_schedule_checks_of_the_live_subscription() => _observer.HasDepartureChecksScheduled.ShouldBeTrue();
}
