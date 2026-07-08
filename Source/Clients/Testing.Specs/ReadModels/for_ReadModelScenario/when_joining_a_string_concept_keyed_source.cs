// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that a <c>[Join]</c> whose source event is keyed by a string concept (an organization number)
/// resolves — the join key must keep its string value rather than being force-converted to the joining read
/// model's Guid identifier.
/// </summary>
public class when_joining_a_string_concept_keyed_source : Specification
{
    ReadModelScenario<EngagementSummary> _scenario;
    EventSourceId _engagementId;
    EventSourceId _orgNumber;

    void Establish()
    {
        _scenario = new ReadModelScenario<EngagementSummary>();
        _engagementId = new EventSourceId(Guid.NewGuid());
        _orgNumber = new EventSourceId("999888777");
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_engagementId).Events(new EngagementStarted(new OrgNumber("999888777")));
        await _scenario.Given.ForEventSource(_orgNumber).Events(new CompanyRegistered("Acme Corp"));
    }

    [Fact] void should_have_an_instance() => _scenario.InstanceForEventSourceId(_engagementId).ShouldNotBeNull();
    [Fact] void should_keep_the_org_number() => _scenario.InstanceForEventSourceId(_engagementId)!.CustomerOrgNumber.Value.ShouldEqual("999888777");
    [Fact] void should_join_the_company_name() => _scenario.InstanceForEventSourceId(_engagementId)!.CustomerName.ShouldEqual("Acme Corp");
}
