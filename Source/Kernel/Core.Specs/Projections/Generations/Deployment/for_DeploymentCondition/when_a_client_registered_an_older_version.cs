// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_a_client_registered_an_older_version : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.1.9")];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_unsatisfied() => _result.State.ShouldEqual(DeploymentConditionState.Unsatisfied);
    [Fact] void should_not_match_the_minimum() => _result.Clients.Single().MatchesMinimumVersion.ShouldBeFalse();
    [Fact] void should_explain_the_older_version() => _result.Clients.Single().Status.ShouldEqual(DeploymentClientStatus.BelowMinimumVersion);
    [Fact] void should_identify_the_version() => _result.Clients.Single().RegisteredVersion.ShouldEqual("1.1.9");
}
