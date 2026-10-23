// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition;

public class when_clients_declare_mixed_versions : given.a_deployment_condition
{
    void Establish() => _registrations = [Registration("1.3.0"), Registration("1.1.0") with { ConnectionId = Cratis.Chronicle.Concepts.Clients.ConnectionId.New() }];

    void Because() => _result = _condition.Evaluate(_eventStore, _projection, _minimumVersion, _now, _gracePeriod, _observationDuration, _registrations);

    [Fact] void should_be_unsatisfied() => _result.State.ShouldEqual(DeploymentConditionState.Unsatisfied);
    [Fact] void should_include_both_clients() => _result.Clients.Count().ShouldEqual(2);
    [Fact] void should_include_the_qualifying_client() => _result.Clients.Single(client => client.ConnectionId == _connection).MatchesMinimumVersion.ShouldBeTrue();
    [Fact] void should_include_the_blocking_client() => _result.Clients.Single(client => client.RegisteredVersion == "1.1.0").Status.ShouldEqual(DeploymentClientStatus.BelowMinimumVersion);
}
