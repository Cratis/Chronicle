// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_the_registration_is_inside_the_grace_period : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.2.0") with { RegisteredAt = _now - _gracePeriod + TimeSpan.FromTicks(1) }];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_unsatisfied() => _result.State.ShouldEqual(DeploymentConditionState.Unsatisfied);
    [Fact] void should_match_the_minimum() => _result.Clients.Single().MatchesMinimumVersion.ShouldBeTrue();
    [Fact] void should_explain_the_wait() => _result.Clients.Single().Status.ShouldEqual(DeploymentClientStatus.GracePeriod);
}
