// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

/// <summary>
/// Identifies a durable initialization step.
/// </summary>
public enum GenerationInitializationStep
{
    /// <summary>
    /// No initialization step has completed.
    /// </summary>
    None = 0,

    /// <summary>
    /// FloorVerified.
    /// </summary>
    FloorVerified = 1,

    /// <summary>
    /// LegacyWorkReconciled.
    /// </summary>
    LegacyWorkReconciled = 2,

    /// <summary>
    /// MaterializationAdopted.
    /// </summary>
    MaterializationAdopted = 3,

    /// <summary>
    /// BindingEpochSet.
    /// </summary>
    BindingEpochSet = 4,

    /// <summary>
    /// Enabled.
    /// </summary>
    Enabled = 5
}
