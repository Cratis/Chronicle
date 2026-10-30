// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_a_version_is_unparseable : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("not-a-version")];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_unsatisfied() => _result.State.ShouldEqual(DeploymentConditionState.Unsatisfied);
    [Fact] void should_explain_the_invalid_version() => _result.Clients.Single().Status.ShouldEqual(DeploymentClientStatus.InvalidVersion);
    [Fact] void should_preserve_the_reported_version() => _result.Clients.Single().RegisteredVersion.ShouldEqual("not-a-version");
    [Fact] void should_not_match_the_minimum() => _result.Clients.Single().MatchesMinimumVersion.ShouldBeFalse();
}
