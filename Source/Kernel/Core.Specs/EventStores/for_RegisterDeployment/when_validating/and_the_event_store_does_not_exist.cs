// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_validating;

public class and_the_event_store_does_not_exist : given.a_registration
{
    void Establish() => _storage.HasEventStore("the-store").Returns(false);
    async Task Because() => _result = await _scenario.Validate(new RegisterDeployment("the-store", 10UL, "fingerprint"));

    [Fact] void should_reject() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_preserve_the_cross_cutting_existence_check() => _result.ValidationResults.Select(_ => _.Message).ShouldContain("Event store does not exist.");
}
