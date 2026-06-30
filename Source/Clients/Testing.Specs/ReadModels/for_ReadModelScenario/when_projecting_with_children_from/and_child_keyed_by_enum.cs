// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_enum : Specification
{
    ReadModelScenario<IncidentLedger> _scenario;
    EventSourceId _ledgerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<IncidentLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new IncidentRecorded(Severity.Low, 1m),
                new IncidentRecorded(Severity.High, 2m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_incident_lines() => _scenario.Instance!.Incidents.Count().ShouldEqual(2);
    [Fact] void should_map_first_incident_key() => _scenario.Instance!.Incidents.First().Level.ShouldEqual(Severity.Low);
    [Fact] void should_map_first_incident_amount() => _scenario.Instance!.Incidents.First().Amount.ShouldEqual(1m);
    [Fact] void should_map_second_incident_key() => _scenario.Instance!.Incidents.Last().Level.ShouldEqual(Severity.High);
}
