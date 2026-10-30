// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// Evaluates a snapshot of deployment evidence without querying grains or performing promotion.
/// </summary>
public interface IDeploymentCondition
{
    /// <summary>
    /// Evaluates whether every declaring connection has held at least the minimum version for the grace period.
    /// </summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="projection">The projection.</param>
    /// <param name="minimumVersion">The minimum definition registration version.</param>
    /// <param name="now">The evaluation time.</param>
    /// <param name="gracePeriod">The configured deployment condition grace period.</param>
    /// <param name="observationDuration">How long this silo has continuously observed clients since starting.</param>
    /// <param name="registrations">The current connected-client declaration snapshot.</param>
    /// <returns>The outcome and per-client evidence. An empty declaring set never satisfies the condition.</returns>
    DeploymentConditionResult Evaluate(
        EventStoreName eventStore,
        ProjectionId projection,
        SemanticVersion minimumVersion,
        DateTimeOffset now,
        TimeSpan gracePeriod,
        TimeSpan observationDuration,
        IEnumerable<ProjectionRegistration> registrations);
}
