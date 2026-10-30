// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations.Deployment;

/// <summary>
/// The outcome of evaluating deployment evidence without performing promotion.
/// </summary>
public enum DeploymentConditionState
{
    /// <summary>
    /// There is insufficient post-restart observation time to establish the condition.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// No client declares the projection, or at least one declaration does not satisfy the condition.
    /// </summary>
    Unsatisfied = 1,

    /// <summary>
    /// All declaring clients have held a qualifying version for the grace period.
    /// </summary>
    Satisfied = 2
}
