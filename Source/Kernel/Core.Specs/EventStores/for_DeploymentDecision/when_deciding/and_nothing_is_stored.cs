// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventStores.for_DeploymentDecision.when_deciding;

public class and_nothing_is_stored : Specification
{
    DeploymentDecision _result;

    void Because() => _result = DeploymentDecision.Decide(DeploymentState.Empty, 1UL, "first");

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_report_the_previous_state() => _result.Stored.ShouldEqual(DeploymentState.Empty);
    [Fact] void should_record_the_deployment() => _result.ToRecord.ShouldEqual(new DeploymentState(1UL, "first"));
}
