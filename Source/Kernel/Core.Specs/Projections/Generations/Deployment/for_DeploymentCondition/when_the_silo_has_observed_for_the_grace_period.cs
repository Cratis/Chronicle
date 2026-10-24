// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_the_silo_has_observed_for_the_grace_period : given.a_deployment_condition
{
    void Establish() => _observationDuration = _gracePeriod;

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_satisfied() => _result.State.ShouldEqual(DeploymentConditionState.Satisfied);
}
