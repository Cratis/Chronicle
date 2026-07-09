// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// A read model carrying a <c>[Join]</c> whose source event is keyed by a string concept (an organization
/// number) both materializes and enriches: seeding the string-keyed source no longer force-converts the join
/// key to the read model's Guid identifier and crashes the whole scenario, and the joined value is backfilled
/// onto the root document — parity with a Guid-keyed join.
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

    [Fact] void should_materialize_the_read_model() => _scenario.InstanceForEventSourceId(_engagementId).ShouldNotBeNull();
    [Fact] void should_keep_the_string_org_number() => _scenario.InstanceForEventSourceId(_engagementId)!.CustomerOrgNumber.Value.ShouldEqual("999888777");
    [Fact] void should_backfill_the_joined_customer_name() => _scenario.InstanceForEventSourceId(_engagementId)!.CustomerName.ShouldEqual("Acme Corp");
}
