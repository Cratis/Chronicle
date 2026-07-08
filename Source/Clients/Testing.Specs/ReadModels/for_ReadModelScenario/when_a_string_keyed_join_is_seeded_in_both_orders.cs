// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Seeds a string-concept-keyed <c>[Join]</c> in both orders — entity-then-source and source-then-entity —
/// and asserts the company name backfills the same way regardless of order. The source-first order is what
/// forces the join key to stay a string (the org number) rather than the read model's Guid identifier.
/// </summary>
public class when_a_string_keyed_join_is_seeded_in_both_orders : Specification
{
    EngagementSummary? _entityFirst;
    EngagementSummary? _sourceFirst;
    Guid _engagementGuid;

    void Establish() => _engagementGuid = Guid.NewGuid();

    async Task Because()
    {
        _entityFirst = await Project(entityFirst: true);
        _sourceFirst = await Project(entityFirst: false);
    }

    [Fact] void should_resolve_the_entity_when_seeded_entity_first() => _entityFirst.ShouldNotBeNull();
    [Fact] void should_resolve_the_entity_when_seeded_source_first() => _sourceFirst.ShouldNotBeNull();
    [Fact] void should_join_the_same_company_name() => _entityFirst!.CustomerName.ShouldEqual(_sourceFirst!.CustomerName);
    [Fact] void should_join_the_company_name_regardless_of_order() => _entityFirst!.CustomerName.ShouldEqual("Acme Corp");

    async Task<EngagementSummary?> Project(bool entityFirst)
    {
        var scenario = new ReadModelScenario<EngagementSummary>();
        var engagementId = new EventSourceId(_engagementGuid);
        var orgNumber = new EventSourceId("999888777");

        if (entityFirst)
        {
            await scenario.Given.ForEventSource(engagementId).Events(new EngagementStarted(new OrgNumber("999888777")));
            await scenario.Given.ForEventSource(orgNumber).Events(new CompanyRegistered("Acme Corp"));
        }
        else
        {
            await scenario.Given.ForEventSource(orgNumber).Events(new CompanyRegistered("Acme Corp"));
            await scenario.Given.ForEventSource(engagementId).Events(new EngagementStarted(new OrgNumber("999888777")));
        }

        return scenario.InstanceForEventSourceId(engagementId);
    }
}
