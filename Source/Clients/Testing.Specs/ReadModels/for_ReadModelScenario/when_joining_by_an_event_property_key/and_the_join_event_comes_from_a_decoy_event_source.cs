// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_by_an_event_property_key;

public class and_the_join_event_comes_from_a_decoy_event_source : given.accounts
{
    async Task Because() => await _scenario.Given.ForEventSource("decoy").Events(new CompanyNamed("c-1", "Acme"));

    [Fact] void should_name_the_keyed_account() => _scenario.InstanceForEventSourceId(_accountId)!.CompanyName.ShouldEqual("Acme");
    [Fact] void should_not_touch_the_decoy_account() => string.IsNullOrEmpty(_scenario.InstanceForEventSourceId(_decoyAccountId)!.CompanyName).ShouldBeTrue();
    [Fact] void should_not_create_another_instance() => _scenario.Instances.Count.ShouldEqual(2);
}
