// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_no_client_declares_the_projection : given.a_deployment_condition
{
    void Establish() => _registrations = [];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_unsatisfied() => _result.State.ShouldEqual(DeploymentConditionState.Unsatisfied);
    [Fact] void should_have_no_client_evidence() => _result.Clients.ShouldBeEmpty();
}
