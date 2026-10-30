// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_the_registration_has_held_for_the_grace_period : given.a_deployment_condition
{
    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_satisfied() => _result.State.ShouldEqual(DeploymentConditionState.Satisfied);
    [Fact] void should_identify_the_connection() => _result.Clients.Single().ConnectionId.ShouldEqual(_connection);
    [Fact] void should_match_the_minimum() => _result.Clients.Single().MatchesMinimumVersion.ShouldBeTrue();
    [Fact] void should_report_satisfied_evidence() => _result.Clients.Single().Status.ShouldEqual(DeploymentClientStatus.Satisfied);
}
