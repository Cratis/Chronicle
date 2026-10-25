// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_the_generation_is_equal_with_a_different_fingerprint : Specification
{
    DeploymentDecision _result;

    void Because() => _result = DeploymentDecision.Decide(new(10UL, "previous"), 10UL, "latest");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_record_the_new_fingerprint() => _result.ToRecord.ShouldEqual(new DeploymentState(10UL, "latest"));
}
