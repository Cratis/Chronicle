// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Discovery;

namespace Cratis.Chronicle.for_EventStore.when_discovering_all;

public class should_publish_the_discovery_report : when_registering_all.given.an_event_store_with_identity_drift
{
    async Task Because() => await _eventStore.DiscoverAll();

    [Fact] void should_publish_a_completed_report() => _eventStore.Discovery.HasRun.ShouldBeTrue();
    [Fact] void should_publish_discovered_artifact_ids() => _eventStore.Discovery.OfKind(ArtifactKind.Reactor).Single().Id.ShouldEqual("Renamed.SendEmail");
    [Fact] void should_expose_the_same_report_through_the_interface() => ((IEventStore)_eventStore).Discovery.ShouldEqual(_eventStore.Discovery);
}
