// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_the_generation_is_higher : Specification
{
    DeploymentDecision _result;

    void Because() => _result = DeploymentDecision.Decide(new(10UL, "same"), 11UL, "same");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_record_the_higher_generation() => _result.ToRecord.ShouldEqual(new DeploymentState(11UL, "same"));
}
