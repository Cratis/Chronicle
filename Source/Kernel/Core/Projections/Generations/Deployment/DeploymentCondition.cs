// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// Pure deployment evidence evaluator, discovered through the kernel's interface binding convention.
/// </summary>
public class DeploymentCondition : IDeploymentCondition
{
    /// <inheritdoc/>
    public DeploymentConditionResult Evaluate(
        EventStoreName eventStore,
        ProjectionId projection,
        SemanticVersion minimumVersion,
        DateTimeOffset now,
        TimeSpan gracePeriod,
        TimeSpan observationDuration,
        IEnumerable<ProjectionRegistration> registrations)
    {
        var clients = registrations
            .Where(registration => registration.EventStore == eventStore && registration.Projection == projection)
            .Select(registration => Evidence(registration, minimumVersion, now, gracePeriod))
            .ToArray();
        return new(State(clients, gracePeriod, observationDuration), clients);
    }

    static DeploymentClientEvidence Evidence(ProjectionRegistration registration, SemanticVersion minimumVersion, DateTimeOffset now, TimeSpan gracePeriod)
    {
        if (string.IsNullOrWhiteSpace(registration.RegisteredVersion))
        {
            return new(registration.ConnectionId, registration.RegisteredVersion, false, DeploymentClientStatus.MissingVersion);
        }

        if (!SemanticVersion.TryParse(registration.RegisteredVersion, out var version))
        {
            return new(registration.ConnectionId, registration.RegisteredVersion, false, DeploymentClientStatus.InvalidVersion);
        }

        if (version! < minimumVersion)
        {
            return new(registration.ConnectionId, registration.RegisteredVersion, false, DeploymentClientStatus.BelowMinimumVersion);
        }

        var status = now - registration.RegisteredAt < gracePeriod ? DeploymentClientStatus.GracePeriod : DeploymentClientStatus.Satisfied;
        return new(registration.ConnectionId, registration.RegisteredVersion, true, status);
    }

    static DeploymentConditionState State(DeploymentClientEvidence[] clients, TimeSpan gracePeriod, TimeSpan observationDuration)
    {
        if (clients.Length == 0) return DeploymentConditionState.Unsatisfied;
        if (observationDuration < gracePeriod) return DeploymentConditionState.Unknown;
        return clients.All(client => client.Status == DeploymentClientStatus.Satisfied) ? DeploymentConditionState.Satisfied : DeploymentConditionState.Unsatisfied;
    }
}
