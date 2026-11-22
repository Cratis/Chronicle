// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_going_dormant;

/// <summary>
/// A replica's registration replaced the registration of another replica: the record names that one as a standby, so the
/// observer is released like one that has a single client. It used to stay resident with its watchdog, queue entry and stream.
/// </summary>
public class when_the_observer_has_a_standby : given.a_client_observer_with_a_standby
{
    async Task Because()
    {
        await RunWatchdogAfter(TimeSpan.Zero);
        await RunWatchdogAfter(_dormantPeriod);
    }

    [Fact] void should_release_the_activation() => ReleaseRequests.ShouldEqual(1);
    [Fact] async Task should_know_the_standby() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeTrue();
    [Fact] void should_record_only_the_active_client_as_a_target() => Recorded!.Targets.Select(target => target.ConnectionId).ShouldContainOnly(_activeClient.ConnectionId.Value);
    [Fact] void should_record_the_standby() => Recorded!.Standbys!.Select(standby => standby.ConnectionId).ShouldContainOnly(_client.ConnectionId.Value);
    [Fact] void should_record_the_event_types_the_standby_registered() => Recorded!.Standbys![0].EventTypes.ShouldContainOnly($"{event_type.Id.Value}+{event_type.Generation.Value}+{event_type.Tombstone}");
}
