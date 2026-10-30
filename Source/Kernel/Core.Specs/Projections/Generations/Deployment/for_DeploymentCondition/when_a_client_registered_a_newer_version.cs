// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_a_client_registered_a_newer_version : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.3.0")];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_satisfied() => _result.State.ShouldEqual(DeploymentConditionState.Satisfied);
    [Fact] void should_match_the_minimum() => _result.Clients.Single().MatchesMinimumVersion.ShouldBeTrue();
}
