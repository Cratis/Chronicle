// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_no_generation_is_sent : Specification
{
    DeploymentState _stored;
    DeploymentDecision _result;

    void Establish() => _stored = new(10UL, "previous");
    void Because() => _result = DeploymentDecision.Decide(_stored, 0UL, "latest");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_keep_the_generation_and_record_the_fingerprint() => _result.ToRecord.ShouldEqual(new DeploymentState(10UL, "latest"));
}
