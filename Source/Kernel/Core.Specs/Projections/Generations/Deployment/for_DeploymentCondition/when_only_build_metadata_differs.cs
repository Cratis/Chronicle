// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_only_build_metadata_differs : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.2.0+build.42")];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_satisfied() => _result.State.ShouldEqual(DeploymentConditionState.Satisfied);
}
