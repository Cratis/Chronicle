// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for converting <see cref="Contracts.Observation.AppliedThroughOutcome"/> to <see cref="AppliedThroughOutcome"/>.
/// </summary>
internal static class AppliedThroughOutcomeConverters
{
    /// <summary>
    /// Convert to client.
    /// </summary>
    /// <param name="outcome"><see cref="Contracts.Observation.AppliedThroughOutcome"/> to convert from.</param>
    /// <returns>Converted <see cref="AppliedThroughOutcome"/>.</returns>
    public static AppliedThroughOutcome ToClient(this Contracts.Observation.AppliedThroughOutcome outcome) =>
        outcome switch
        {
            Contracts.Observation.AppliedThroughOutcome.Ready => AppliedThroughOutcome.Ready,
            Contracts.Observation.AppliedThroughOutcome.TimedOut => AppliedThroughOutcome.TimedOut,
            Contracts.Observation.AppliedThroughOutcome.Unavailable => AppliedThroughOutcome.Unavailable,
            Contracts.Observation.AppliedThroughOutcome.Failed => AppliedThroughOutcome.Failed,
            Contracts.Observation.AppliedThroughOutcome.Replaying => AppliedThroughOutcome.Replaying,
            Contracts.Observation.AppliedThroughOutcome.Quarantined => AppliedThroughOutcome.Quarantined,
            _ => AppliedThroughOutcome.Unknown
        };
}
