// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_the_registration_is_after_the_grace_period : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.2.0") with { RegisteredAt = _now - TimeSpan.FromMinutes(5) }];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_satisfied() => _result.State.ShouldEqual(DeploymentConditionState.Satisfied);
}
