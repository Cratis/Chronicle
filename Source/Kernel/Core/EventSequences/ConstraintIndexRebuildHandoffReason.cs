// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Why a constraint rebuild handed ownership back to the event sequence.
/// </summary>
internal enum ConstraintIndexRebuildHandoffReason
{
    /// <summary>
    /// The step handed over confirmed progress, or nothing written.
    /// </summary>
    Rebuilt = 0,

    /// <summary>
    /// The step handed over without confirmed progress, through cancellation or supersession.
    /// </summary>
    ProgressUnknown = 1,

    /// <summary>
    /// Liveness detected that the step ended without a confirmed handoff; progress is unknown.
    /// </summary>
    Failed = 2
}
