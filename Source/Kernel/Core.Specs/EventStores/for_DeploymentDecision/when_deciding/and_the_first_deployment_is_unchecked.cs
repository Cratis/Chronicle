// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_the_first_deployment_is_unchecked : Specification
{
    DeploymentDecision _result;

    void Because() => _result = DeploymentDecision.Decide(DeploymentState.Empty, 0UL, "first");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_record_without_a_generation() => _result.ToRecord.ShouldEqual(new DeploymentState(0UL, "first"));
}
