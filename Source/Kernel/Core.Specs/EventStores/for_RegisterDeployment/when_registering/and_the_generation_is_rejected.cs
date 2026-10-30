// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_RegisterDeployment.when_registering;

public class and_the_generation_is_rejected : given.a_registration
{
    void Establish() => _registry.Register(10UL, "fingerprint").Returns(new DeploymentDecision(false, new(11UL, "newer"), null));
    async Task Because() => _result = await _scenario.Execute(new RegisterDeployment("the-store", 10UL, "fingerprint"));

    [Fact] void should_fail_as_a_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_generation_member() => _result.ValidationResults.Single().Members.ShouldContainOnly(["generation"]);
    [Fact] void should_explain_the_rejection() => _result.ValidationResults.Single().Message.ShouldEqual("Deployment generation 10 for event store 'the-store' is lower than the stored generation 11.");
}
