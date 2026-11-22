// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

/// <summary>
/// Pins what a replica's registration does to dormancy today: the observer keeps the registration it replaced and stays resident.
/// </summary>
public class when_the_observer_has_a_standby : given.a_client_observer_with_a_standby
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    [Fact] void should_not_release_the_activation() => ReleaseRequests.ShouldEqual(0);
    [Fact] async Task should_know_the_standby() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeTrue();
    [Fact] void should_record_only_the_active_client() => Recorded!.Targets.Select(target => target.ConnectionId).ShouldContainOnly(_activeClient.ConnectionId.Value);
}
