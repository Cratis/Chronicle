// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Classifies the result of an individual constraint-index attempt.
/// </summary>
public enum ConstraintIndexUpdateOutcome
{
    /// <summary>
    /// The update succeeded.
    /// </summary>
    Applied = 0,

    /// <summary>
    /// The store settled a competing claim.
    /// </summary>
    Settled = 1,

    /// <summary>
    /// The event's closing effect cannot be resolved.
    /// </summary>
    SettledWithError = 2,

    /// <summary>
    /// The attempt failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// The enclosing turn budget ended without confirming this attempt.
    /// </summary>
    NotAttempted = 4,

    /// <summary>
    /// The one-second storage deadline ended without confirmation.
    /// </summary>
    Abandoned = 5
}
