// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key;

public class and_the_join_key_is_the_event_source_id : Specification
{
    ReadModelScenario<EventSourceAccountSummary> _scenario;
    EventSourceId _accountId;
    EventSourceId _decoyAccountId;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<EventSourceAccountSummary>();
        _accountId = EventSourceId.New();
        _decoyAccountId = EventSourceId.New();
        await _scenario.Given.ForEventSource(_accountId).Events(new AccountOpened("c-1"));
        await _scenario.Given.ForEventSource(_decoyAccountId).Events(new AccountOpened("decoy"));
    }

    async Task Because() => await _scenario.Given.ForEventSource("decoy").Events(new CompanyNamed("c-1", "Acme"));

    [Fact] void should_name_the_account_matching_the_event_source() => _scenario.InstanceForEventSourceId(_decoyAccountId)!.CompanyName.ShouldEqual("Acme");
    [Fact] void should_not_use_the_event_property() => string.IsNullOrEmpty(_scenario.InstanceForEventSourceId(_accountId)!.CompanyName).ShouldBeTrue();
    [Fact] void should_not_create_another_instance() => _scenario.Instances.Count.ShouldEqual(2);
}
