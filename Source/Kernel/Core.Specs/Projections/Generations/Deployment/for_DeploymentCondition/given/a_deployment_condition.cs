// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Projections.Generations.Deployment.for_DeploymentCondition.given;

public class a_deployment_condition : Specification
{
    protected EventStoreName _eventStore = "orders";
    protected ProjectionId _projection = "order-summary";
    protected ConnectionId _connection = ConnectionId.New();
    protected SemanticVersion _minimumVersion = "1.2.0";
    protected DateTimeOffset _now = new(2026, 7, 9, 12, 0, 0, TimeSpan.Zero);
    protected TimeSpan _gracePeriod = TimeSpan.FromSeconds(60);
    protected TimeSpan _observationDuration = TimeSpan.FromMinutes(10);
    protected IEnumerable<ProjectionRegistration> _registrations;
    protected IDeploymentCondition _condition;
    protected DeploymentConditionResult _result;

    void Establish()
    {
        _condition = new DeploymentCondition();
        _registrations = [Registration("1.2.0")];
    }

    protected ProjectionRegistration Registration(string? version) => new(_connection, _eventStore, _projection, version, _now - _gracePeriod);
}
