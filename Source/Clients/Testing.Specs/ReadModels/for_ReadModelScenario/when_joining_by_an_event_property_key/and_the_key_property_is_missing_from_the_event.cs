// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key;

public class and_the_key_property_is_missing_from_the_event : Specification
{
    ReadModelScenario<AccountSummary> _scenario;
    EventSourceId _accountId;

    async Task Establish()
    {
        _scenario = new ReadModelScenario<AccountSummary>();
        _accountId = EventSourceId.New();
        await _scenario.Given.ForEventSource(_accountId).Events(new AccountOpened("c-2"));
    }

    async Task Because() => await _scenario.Given.ForEventSource("c-2").Events(new CompanyNamed(null, "Acme"));

    [Fact] void should_fall_back_to_the_event_source_id() => _scenario.InstanceForEventSourceId(_accountId)!.CompanyName.ShouldEqual("Acme");
    [Fact] void should_not_create_another_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
