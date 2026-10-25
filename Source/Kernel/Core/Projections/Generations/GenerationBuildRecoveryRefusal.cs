// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Generations;

/// <summary>
/// Identifies why a Building generation cannot recover its persisted intent.
/// </summary>
public enum GenerationBuildRecoveryRefusal
{
    /// <summary>
    /// No original build intent was persisted for the generation.
    /// </summary>
    IntentMissing = 0,

    /// <summary>
    /// The persisted intent conflicts with the generation catalog or immutable claims.
    /// </summary>
    IntentInconsistent = 1,

    /// <summary>
    /// The persisted intent belongs to a key other than the current materialization.
    /// </summary>
    KeySuperseded = 2
}
