// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// A read model carrying a <c>[Join]</c> whose source event is keyed by a string concept (an organization
/// number) is spec-able: seeding the string-keyed source no longer force-converts the join key to the
/// read model's Guid identifier and crashes the whole scenario. The read model materializes and its own
/// properties can be asserted.
/// </summary>
/// <remarks>
/// The joined value itself (<c>CustomerName</c>) is not asserted here: <c>ReadModelScenario</c> does not
/// materialize root-level join enrichment (a pre-existing harness limitation that applies to Guid-keyed
/// joins too — the real engine enriches via its join read-back, covered by the out-of-process integration
/// specs). The point of this spec is that a string-concept-keyed join source no longer makes the read model
/// entirely un-spec-able.
/// </remarks>
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
}
