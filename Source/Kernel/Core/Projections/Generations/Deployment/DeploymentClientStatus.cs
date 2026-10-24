// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// Explains a declaring client's contribution to the deployment condition.
/// </summary>
public enum DeploymentClientStatus
{
    /// <summary>
    /// The declaration has no registration version.
    /// </summary>
    MissingVersion = 0,

    /// <summary>
    /// The registered version cannot be parsed as a semantic version.
    /// </summary>
    InvalidVersion = 1,

    /// <summary>
    /// The registered version is below the required minimum.
    /// </summary>
    BelowMinimumVersion = 2,

    /// <summary>
    /// The version qualifies, but has not been held for the grace period.
    /// </summary>
    GracePeriod = 3,

    /// <summary>
    /// The client has held a qualifying version for the grace period.
    /// </summary>
    Satisfied = 4
}
