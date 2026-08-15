// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario;

/// <summary>
/// A reactor keyed on the delivery identity has to be testable in the scenario, which means the scenario must give
/// every event it feeds a delivery of its own. Handing them all the same identity would make an idempotent reactor
/// skip every event after the first, and the spec would report the reactor broken when it is the harness that is.
/// </summary>
public class when_a_reactor_reads_the_delivery_identity : Specification
{
    ReactorScenario<DeliveryRecordingReactor> _scenario;
    DeliveryLog _log;
    EventSourceId _vibeId;

    void Establish()
    {
        _vibeId = EventSourceId.New();
        _log = new DeliveryLog();
        _scenario = new ReactorScenario<DeliveryRecordingReactor>();
        _scenario.Services.AddSingleton(_log);
    }

    async Task Because() => await _scenario.Given.ForEventSource(_vibeId).Events(new VibeStarted("Ada"), new VibeStarted("Grace"));

    [Fact] void should_hand_the_reactor_a_delivery_for_every_event() => _log.Deliveries.Count.ShouldEqual(2);
    [Fact] void should_give_each_event_its_own_identity() => _log.Deliveries[0].ShouldNotEqual(_log.Deliveries[1]);
    [Fact] void should_identify_the_partition_the_events_were_fed_to() => _log.Deliveries[0].Value.ShouldContain(_vibeId.Value);
}
